using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerPathController : MonoBehaviour, ICheckpointPathActor
{
    [SerializeField] private PathGraph graph;
    [SerializeField] private PathClickFeedback clickFeedback;
    [SerializeField] private Camera clickCamera;
    [SerializeField] private LayerMask walkableMask;
    [SerializeField, Min(0.01f)] private float speed = 3f;
    [SerializeField, Min(0f)] private float turnSpeed = 12f;
    [SerializeField, Min(0.1f)] private float clickSnapDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float startSnapDistance = 2f;
    [SerializeField] private float killHeight = -5f;

    private CharacterController characterController;
    private List<PathNode> route;
    private int nextNodeIndex;
    private int observedGraphVersion;
    private bool stopAfterCurrentEdge;
    private float blockedSeconds;
    private Vector3 currentNodePosition;

    public PathNode CurrentNode { get; private set; }
    public bool IsMoving => route != null;
    public event Action<Vector3> TravelDisplacement;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (clickFeedback == null) clickFeedback = GetComponent<PathClickFeedback>();
        characterController.minMoveDistance = 0f;
        if (graph == null)
        {
            graph = FindFirstObjectByType<PathGraph>();
        }
        if (walkableMask.value == 0)
        {
            walkableMask = LayerMask.GetMask("Walkable");
        }
    }

    private void Start()
    {
        SynchronizeToGraph();
    }

    private void Update()
    {
        if (transform.position.y < killHeight)
        {
            CancelRoute();
            FindFirstObjectByType<CheckpointService>()?.NotifyPlayerFailure();
            return;
        }

        if (IsMoving)
        {
            Advance(Time.deltaTime);
            return;
        }

        SynchronizeToGraph();
        if (Input.GetMouseButtonDown(0))
        {
            TryMoveToCursor();
        }
    }

    private void OnDisable()
    {
        CancelRoute();
    }

    public bool TryMoveToCursor()
    {
        if (IsMoving) return false;
        var cameraToUse = clickCamera != null ? clickCamera : Camera.main;
        if (cameraToUse == null || walkableMask.value == 0)
        {
            return false;
        }
        var ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 200f, walkableMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        if (graph != null && graph.Board != null)
        {
            var node = graph.FindNodeForCollider(hit.collider);
            if (node != null) return TryMoveTo(node);
            clickFeedback?.ShowRejected(hit.point);
            return false;
        }
        return TryMoveToWorldPoint(hit.point);
    }

    public bool TryMoveToWorldPoint(Vector3 point)
    {
        if (graph == null || IsMoving)
        {
            return false;
        }
        if (graph.Board != null)
        {
            if (graph.Board.TryGetCellAtWorld(point, out var cell) && cell.PlayerAccessible)
                return TryMoveTo(cell.Node);
            clickFeedback?.ShowRejected(point);
            return false;
        }
        return TryMoveTo(graph.FindNearestNode(point, clickSnapDistance));
    }

    public bool TryMoveTo(PathNode destination)
    {
        if (graph == null || IsMoving || destination == null)
        {
            return false;
        }
        graph.Rebuild();
        SynchronizeToGraph();
        if (CurrentNode == null || Vector3.Distance(transform.position, CurrentNode.Position) > 0.1f)
        {
            return false;
        }

        var planned = Pathfinder.FindPath(graph, CurrentNode, destination);
        if (planned == null || planned.Count < 2)
        {
            clickFeedback?.ShowRejected(destination.Position);
            return false;
        }

        // External checkpoint restores can leave a small offset. Start each route on its authored node.
        if (Vector3.Distance(transform.position, CurrentNode.Position) > 0.01f)
        {
            characterController.enabled = false;
            transform.position = CurrentNode.Position;
            characterController.enabled = true;
        }
        route = planned;
        nextNodeIndex = 1;
        observedGraphVersion = graph.Version;
        stopAfterCurrentEdge = false;
        blockedSeconds = 0f;
        clickFeedback?.ShowDestination(destination.Position);
        return true;
    }

    public bool RestoreToNearestNode(Vector3 position, Quaternion rotation)
    {
        if (graph == null)
        {
            return false;
        }
        graph.Rebuild();
        var node = graph.FindNearestNode(position, float.MaxValue);
        if (node == null)
        {
            return false;
        }

        CancelRoute();
        var wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.SetPositionAndRotation(node.Position, rotation);
        characterController.enabled = wasEnabled;
        CurrentNode = node;
        currentNodePosition = node.Position;
        return true;
    }

    private void Advance(float deltaTime)
    {
        if (graph == null || route == null || nextNodeIndex >= route.Count)
        {
            CancelRoute();
            return;
        }

        if (graph.Version != observedGraphVersion)
        {
            observedGraphVersion = graph.Version;
            if (!graph.IsTraversable(route[nextNodeIndex - 1], route[nextNodeIndex]))
            {
                ReturnToLastNode();
                return;
            }
            for (var index = nextNodeIndex; index < route.Count - 1; index++)
            {
                if (!graph.IsTraversable(route[index], route[index + 1]))
                {
                    stopAfterCurrentEdge = true;
                    break;
                }
            }
        }

        var target = route[nextNodeIndex].Position;
        var tangent = target - transform.position;
        // CharacterController's skin keeps a standing capsule slightly above the
        // authored floor. Do not push that tiny vertical offset into the floor
        // on every horizontal step, or the controller can stall against it.
        if (Mathf.Abs(tangent.y) < 0.1f)
        {
            tangent.y = 0f;
        }
        if (tangent.sqrMagnitude > 0.0001f)
        {
            var horizontal = Vector3.ProjectOnPlane(tangent, Vector3.up);
            if (horizontal.sqrMagnitude > 0.0001f)
            {
                var facing = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, facing,
                    1f - Mathf.Exp(-turnSpeed * deltaTime));
            }
            var step = Vector3.ClampMagnitude(tangent, speed * deltaTime);
            var before = transform.position;
            characterController.Move(step);
            PublishAchievedMovement(before);
            blockedSeconds = (transform.position - before).sqrMagnitude < step.sqrMagnitude * 0.0625f
                ? blockedSeconds + deltaTime : 0f;
            if (blockedSeconds >= 0.25f)
            {
                ReturnToLastNode();
                return;
            }
        }

        if (Vector3.ProjectOnPlane(transform.position - target, Vector3.up).sqrMagnitude <= 0.015f * 0.015f
            && Mathf.Abs(transform.position.y - target.y) <= 0.1f)
        {
            characterController.enabled = false;
            transform.position = target;
            characterController.enabled = true;
            CurrentNode = route[nextNodeIndex];
            currentNodePosition = CurrentNode.Position;
            nextNodeIndex++;
            if (stopAfterCurrentEdge || nextNodeIndex >= route.Count)
            {
                CancelRoute();
            }
        }
    }

    private void SynchronizeToGraph()
    {
        if (graph == null || IsMoving)
        {
            return;
        }
        if (graph.Contains(CurrentNode))
        {
            var delta = CurrentNode.Position - currentNodePosition;
            if (delta.sqrMagnitude > 0.00000001f)
            {
                var before = transform.position;
                var wasEnabled = characterController.enabled;
                characterController.enabled = false;
                transform.position += delta;
                characterController.enabled = wasEnabled;
                currentNodePosition = CurrentNode.Position;
                PublishAchievedMovement(before);
            }
        }
        if (!graph.Contains(CurrentNode)
            || Vector3.Distance(transform.position, CurrentNode.Position) > 0.5f)
        {
            CurrentNode = graph.FindNearestNode(transform.position, startSnapDistance);
            currentNodePosition = CurrentNode != null ? CurrentNode.Position : default;
        }
    }

    private void CancelRoute()
    {
        route = null;
        nextNodeIndex = 0;
        stopAfterCurrentEdge = false;
        blockedSeconds = 0f;
        clickFeedback?.HideDestination();
    }

    private void PublishAchievedMovement(Vector3 before)
    {
        var achieved = Vector3.ProjectOnPlane(transform.position - before, Vector3.up);
        if (achieved.sqrMagnitude > 0.00000001f)
        {
            TravelDisplacement?.Invoke(achieved);
        }
    }

    private void ReturnToLastNode()
    {
        var lastNode = route[nextNodeIndex - 1];
        characterController.enabled = false;
        transform.position = lastNode.Position;
        characterController.enabled = true;
        CurrentNode = lastNode;
        currentNodePosition = lastNode.Position;
        CancelRoute();
    }

    private void OnDrawGizmosSelected()
    {
        if (route == null)
        {
            return;
        }
        Gizmos.color = Color.yellow;
        for (var index = nextNodeIndex; index < route.Count; index++)
        {
            if (route[index] != null)
            {
                Gizmos.DrawLine(index == nextNodeIndex ? transform.position : route[index - 1].Position,
                    route[index].Position);
            }
        }
    }
}
