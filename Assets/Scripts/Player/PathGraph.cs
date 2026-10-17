using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PathEdge
{
    public string Id;
    public PathNode From;
    public PathNode To;
    public bool Bidirectional = true;
    public bool Enabled = true;
    public Collider RequiredCollider;
    public bool RequiredColliderEnabled = true;
    [Min(0f)] public float MaxConnectionDistance;

    public bool IsAvailable => Enabled
        && From != null && To != null && From != To
        && From.isActiveAndEnabled && To.isActiveAndEnabled
        && (RequiredCollider == null || RequiredCollider.enabled == RequiredColliderEnabled)
        && (MaxConnectionDistance <= 0f
            || Vector3.Distance(From.Position, To.Position) <= MaxConnectionDistance);
}

public readonly struct PathNeighbor
{
    public readonly PathNode Node;
    public readonly float Cost;

    public PathNeighbor(PathNode node, float cost)
    {
        Node = node;
        Cost = cost;
    }
}

[DisallowMultipleComponent]
public sealed class PathGraph : MonoBehaviour
{
    [SerializeField] public PathNode[] Nodes = Array.Empty<PathNode>();
    [SerializeField] public PathEdge[] Edges = Array.Empty<PathEdge>();
    [SerializeField] private GridBoard board;
    [SerializeField] private float gizmoNodeRadius = 0.15f;

    private readonly Dictionary<PathNode, List<PathNeighbor>> adjacency = new Dictionary<PathNode, List<PathNeighbor>>();
    private int fingerprint;

    public int Version { get; private set; }
    public event Action Changed;
    public GridBoard Board
    {
        get => board;
        set => board = value;
    }

    private void Awake()
    {
        if (board == null) board = FindFirstObjectByType<GridBoard>();
        Rebuild();
    }

    private void LateUpdate()
    {
        if (ComputeFingerprint() != fingerprint)
        {
            Rebuild();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Rebuild();
        }
    }

    public void Rebuild()
    {
        adjacency.Clear();
        if (Nodes != null)
        {
            foreach (var node in Nodes)
            {
                var cell = node != null ? node.GetComponentInParent<GridCell>() : null;
                if (node != null && node.isActiveAndEnabled
                    && (cell == null || cell.PlayerAccessible) && !adjacency.ContainsKey(node))
                {
                    adjacency.Add(node, new List<PathNeighbor>());
                }
            }
        }

        if (board != null)
        {
            foreach (var cell in board.Cells)
            {
                if (cell != null && cell.PlayerAccessible && cell.Node.isActiveAndEnabled
                    && !adjacency.ContainsKey(cell.Node))
                {
                    adjacency.Add(cell.Node, new List<PathNeighbor>());
                }
            }
            foreach (var cell in board.Cells)
            {
                if (cell == null || !cell.PlayerAccessible || !adjacency.ContainsKey(cell.Node)) continue;
                foreach (var neighbor in board.GetOrthogonalNeighbors(cell))
                {
                    if (neighbor == null || !neighbor.PlayerAccessible
                        || !adjacency.ContainsKey(neighbor.Node)
                        || HasManualEdgeBetween(cell.Node, neighbor.Node)
                        || Vector3.Distance(cell.Node.Position, neighbor.Node.Position) > board.CellPitch * 1.05f)
                    {
                        continue;
                    }
                    AddNeighbor(cell.Node, neighbor.Node);
                }
            }
        }

        if (Edges != null)
        {
            foreach (var edge in Edges)
            {
                if (edge == null || !edge.IsAvailable
                    || !adjacency.ContainsKey(edge.From) || !adjacency.ContainsKey(edge.To))
                {
                    continue;
                }

                AddNeighbor(edge.From, edge.To);
                if (edge.Bidirectional)
                {
                    AddNeighbor(edge.To, edge.From);
                }
            }
        }

        fingerprint = ComputeFingerprint();
        Version++;
        Changed?.Invoke();
    }

    public bool SetEdgeEnabled(string id, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(id) || Edges == null)
        {
            return false;
        }

        foreach (var edge in Edges)
        {
            if (edge != null && edge.Id == id)
            {
                if (edge.Enabled != enabled)
                {
                    edge.Enabled = enabled;
                    Rebuild();
                }
                return true;
            }
        }
        return false;
    }

    public bool Contains(PathNode node)
    {
        return node != null && adjacency.ContainsKey(node);
    }

    public IReadOnlyList<PathNeighbor> GetOutgoing(PathNode node)
    {
        return node != null && adjacency.TryGetValue(node, out var neighbors)
            ? neighbors : Array.Empty<PathNeighbor>();
    }

    public bool IsTraversable(PathNode from, PathNode to)
    {
        foreach (var neighbor in GetOutgoing(from))
        {
            if (neighbor.Node == to)
            {
                return true;
            }
        }
        return false;
    }

    public PathNode FindNearestNode(Vector3 position, float maxDistance)
    {
        PathNode nearest = null;
        var best = Mathf.Max(0f, maxDistance) * Mathf.Max(0f, maxDistance);
        foreach (var pair in adjacency)
        {
            var distance = (pair.Key.Position - position).sqrMagnitude;
            if (distance <= best)
            {
                nearest = pair.Key;
                best = distance;
            }
        }
        return nearest;
    }

    public PathNode FindNodeForCollider(Collider collider)
    {
        return board != null && board.TryGetCell(collider, out var cell)
            && cell.PlayerAccessible && Contains(cell.Node) ? cell.Node : null;
    }

    private void AddNeighbor(PathNode from, PathNode to)
    {
        var neighbors = adjacency[from];
        foreach (var existing in neighbors)
        {
            if (existing.Node == to) return;
        }
        neighbors.Add(new PathNeighbor(to, Vector3.Distance(from.Position, to.Position)));
    }

    private bool HasManualEdgeBetween(PathNode a, PathNode b)
    {
        if (Edges == null) return false;
        foreach (var edge in Edges)
        {
            if (edge != null && ((edge.From == a && edge.To == b)
                || (edge.From == b && edge.To == a))) return true;
        }
        return false;
    }

    private int ComputeFingerprint()
    {
        unchecked
        {
            var hash = 17;
            if (Nodes != null)
            {
                foreach (var node in Nodes)
                {
                    hash = hash * 31 + (node != null ? node.GetInstanceID() : 0);
                    hash = hash * 31 + (node != null ? node.Position.GetHashCode() : 0);
                    hash = hash * 31 + (node != null && node.isActiveAndEnabled ? 1 : 0);
                }
            }
            if (Edges != null)
            {
                foreach (var edge in Edges)
                {
                    hash = hash * 31 + (edge != null && edge.IsAvailable ? 1 : 0);
                }
            }
            if (board != null)
            {
                foreach (var cell in board.Cells)
                {
                    hash = hash * 31 + (cell != null ? cell.GetInstanceID() : 0);
                    hash = hash * 31 + (cell != null && cell.PlayerAccessible ? 1 : 0);
                    hash = hash * 31 + (cell != null && cell.Node != null
                        ? cell.Node.Position.GetHashCode() : 0);
                }
            }
            return hash;
        }
    }

    private void OnDrawGizmos()
    {
        if (board != null)
        {
            foreach (var cell in board.Cells)
            {
                if (cell == null || !cell.PlayerAccessible || cell.Node == null) continue;
                foreach (var neighbor in board.GetOrthogonalNeighbors(cell))
                {
                    if (neighbor == null || !neighbor.PlayerAccessible || neighbor.Node == null
                        || HasManualEdgeBetween(cell.Node, neighbor.Node)
                        || Vector3.Distance(cell.Node.Position, neighbor.Node.Position) > board.CellPitch * 1.05f
                        || neighbor.Coordinate.x < cell.Coordinate.x
                        || neighbor.Coordinate.y < cell.Coordinate.y) continue;
                    Gizmos.color = new Color(0.45f, 0.85f, 0.45f, 0.7f);
                    Gizmos.DrawLine(cell.Node.Position, neighbor.Node.Position);
                }
            }
        }
        if (Nodes != null)
        {
            Gizmos.color = new Color(0.1f, 0.9f, 1f);
            foreach (var node in Nodes)
            {
                if (node != null)
                {
                    Gizmos.DrawSphere(node.Position, gizmoNodeRadius);
                }
            }
        }

        if (Edges == null)
        {
            return;
        }
        foreach (var edge in Edges)
        {
            if (edge == null || edge.From == null || edge.To == null)
            {
                continue;
            }
            Gizmos.color = edge.IsAvailable ? new Color(0.2f, 1f, 0.45f) : new Color(1f, 0.25f, 0.25f);
            var start = edge.From.Position;
            var end = edge.To.Position;
            Gizmos.DrawLine(start, end);
            DrawArrow(start, end);
            if (edge.Bidirectional)
            {
                DrawArrow(end, start);
            }
        }
    }

    private static void DrawArrow(Vector3 from, Vector3 to)
    {
        var direction = (to - from).normalized;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }
        var point = Vector3.Lerp(from, to, 0.65f);
        var side = Vector3.Cross(Vector3.up, direction).normalized;
        if (side.sqrMagnitude < 0.001f)
        {
            side = Vector3.right;
        }
        Gizmos.DrawLine(point, point - direction * 0.24f + side * 0.12f);
        Gizmos.DrawLine(point, point - direction * 0.24f - side * 0.12f);
    }
}
