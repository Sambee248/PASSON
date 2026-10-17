using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class PatrolEnemy : MonoBehaviour, ICheckpointState
{
    [SerializeField] private string checkpointId;
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0f)] private float patrolSpeed = 1.3f;
    [SerializeField, Min(0f)] private float pursuitSpeed = 2f;
    [SerializeField, Min(0f)] private float detectionRange = 6f;
    [SerializeField] private Transform player;
    [SerializeField] private CheckpointService checkpointService;

    private int nextWaypoint;

    public bool IsCleared { get; private set; }
    public string CheckpointId => string.IsNullOrWhiteSpace(checkpointId)
        ? $"{gameObject.scene.name}/{gameObject.name}" : checkpointId;

    public void Configure(Transform[] patrolPoints, Transform playerTarget,
        CheckpointService checkpoints)
    {
        waypoints = patrolPoints;
        player = playerTarget;
        checkpointService = checkpoints;
        nextWaypoint = 0;
    }

    private void Awake()
    {
        if (player == null)
        {
            var actor = GameObject.FindGameObjectWithTag("Player");
            if (actor != null) player = actor.transform;
        }
        if (checkpointService == null) checkpointService = FindFirstObjectByType<CheckpointService>();
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (IsCleared) return;
        var target = CanSeePlayer() ? player : NextPatrolPoint();
        if (target == null) return;
        var speed = target == player ? pursuitSpeed : patrolSpeed;
        var toward = target.position - transform.position;
        toward.y = 0f;
        if (target != player && toward.sqrMagnitude < 0.04f)
        {
            nextWaypoint = (nextWaypoint + 1) % waypoints.Length;
            return;
        }
        if (toward.sqrMagnitude < 0.0001f) return;
        transform.position += toward.normalized * Mathf.Min(speed * Time.deltaTime, toward.magnitude);
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(toward.normalized, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    private Transform NextPatrolPoint() => waypoints != null && waypoints.Length > 0
        ? waypoints[nextWaypoint % waypoints.Length] : null;

    private bool CanSeePlayer()
    {
        if (player == null || Vector3.Distance(transform.position, player.position) > detectionRange)
            return false;
        var from = transform.position + Vector3.up * 0.8f;
        var to = player.position + Vector3.up * 0.8f;
        var direction = to - from;
        return Physics.Raycast(from, direction.normalized, out var hit, direction.magnitude,
            ~0, QueryTriggerInteraction.Ignore)
            && (hit.transform == player || hit.transform.IsChildOf(player));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsCleared && other.CompareTag("Player"))
            checkpointService?.NotifyPlayerFailure();
    }

    public void Clear()
    {
        if (IsCleared) return;
        IsCleared = true;
        foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
    }

    public string CaptureState()
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        var renderers = GetComponentsInChildren<Renderer>(true);
        var state = new SavedState
        {
            position = transform.position,
            rotation = transform.rotation,
            nextWaypoint = nextWaypoint,
            cleared = IsCleared,
            collidersEnabled = Array.ConvertAll(colliders, item => item.enabled),
            renderersEnabled = Array.ConvertAll(renderers, item => item.enabled)
        };
        return JsonUtility.ToJson(state);
    }

    public void RestoreState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        var state = JsonUtility.FromJson<SavedState>(json);
        transform.SetPositionAndRotation(state.position, state.rotation);
        nextWaypoint = state.nextWaypoint;
        IsCleared = state.cleared;
        var colliders = GetComponentsInChildren<Collider>(true);
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < colliders.Length && i < state.collidersEnabled.Length; i++)
            colliders[i].enabled = state.collidersEnabled[i];
        for (var i = 0; i < renderers.Length && i < state.renderersEnabled.Length; i++)
            renderers[i].enabled = state.renderersEnabled[i];
        Physics.SyncTransforms();
    }

    [Serializable]
    private struct SavedState
    {
        public Vector3 position;
        public Quaternion rotation;
        public int nextWaypoint;
        public bool cleared;
        public bool[] collidersEnabled;
        public bool[] renderersEnabled;
    }
}
