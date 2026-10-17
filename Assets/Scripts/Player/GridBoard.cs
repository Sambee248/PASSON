using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GridBoard : MonoBehaviour
{
    private static readonly Vector2Int[] CardinalOffsets =
    {
        Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down
    };

    [SerializeField, Min(0.1f)] private float cellPitch = 1.5f;
    [SerializeField] private GridCell[] cells = Array.Empty<GridCell>();

    private readonly Dictionary<Vector2Int, GridCell> indexedCells = new Dictionary<Vector2Int, GridCell>();

    public IReadOnlyList<GridCell> Cells => cells;
    public float CellPitch => cellPitch;

    private void Awake() => RebuildIndex();

    public void Configure(float pitch, GridCell[] boardCells)
    {
        cellPitch = Mathf.Max(0.1f, pitch);
        cells = boardCells ?? Array.Empty<GridCell>();
        RebuildIndex();
    }

    public void RebuildIndex()
    {
        indexedCells.Clear();
        foreach (var cell in cells)
        {
            if (cell == null) continue;
            if (indexedCells.ContainsKey(cell.Coordinate))
            {
                Debug.LogError($"Duplicate grid coordinate {cell.Coordinate} on {cell.name}", this);
                continue;
            }
            indexedCells.Add(cell.Coordinate, cell);
        }
    }

    public bool TryGetCell(Vector2Int coordinate, out GridCell cell) =>
        indexedCells.TryGetValue(coordinate, out cell);

    public bool TryGetCell(Collider surfaceCollider, out GridCell cell)
    {
        cell = surfaceCollider != null ? surfaceCollider.GetComponentInParent<GridCell>() : null;
        return cell != null && cell.Surface == surfaceCollider
            && indexedCells.TryGetValue(cell.Coordinate, out var indexed) && indexed == cell;
    }

    public bool TryGetCellAtWorld(Vector3 point, out GridCell cell)
    {
        foreach (var candidate in cells)
        {
            if (candidate == null || candidate.Surface == null || !candidate.Surface.enabled) continue;
            var bounds = candidate.Surface.bounds;
            if (point.x >= bounds.min.x && point.x <= bounds.max.x
                && point.z >= bounds.min.z && point.z <= bounds.max.z
                && Mathf.Abs(point.y - bounds.max.y) <= 0.25f)
            {
                cell = candidate;
                return true;
            }
        }
        cell = null;
        return false;
    }

    public IReadOnlyList<GridCell> GetOrthogonalNeighbors(GridCell cell)
    {
        var neighbors = new List<GridCell>(4);
        if (cell == null) return neighbors;
        foreach (var offset in CardinalOffsets)
        {
            if (TryGetCell(cell.Coordinate + offset, out var neighbor) && neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    public bool TryGetShadowSupport(Vector3 feet, out GridCell cell)
    {
        var ray = new Ray(feet + Vector3.up * 0.5f, Vector3.down);
        foreach (var hit in Physics.RaycastAll(ray, 0.8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (TryGetCell(hit.collider, out cell) && cell.ShadowWalkable
                && Mathf.Abs(hit.point.y - feet.y) <= 0.25f)
            {
                return true;
            }
        }
        cell = null;
        return false;
    }

    public bool CanShadowOccupy(Vector3 feet, float radius)
    {
        if (!TryGetShadowSupport(feet, out _)) return false;
        var sampleRadius = Mathf.Max(0f, radius) * 0.9f;
        for (var index = 0; index < 8; index++)
        {
            var angle = index * Mathf.PI / 4f;
            var sample = feet + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * sampleRadius;
            if (!TryGetShadowSupport(sample, out _)) return false;
        }
        return true;
    }

    private void OnDrawGizmos()
    {
        if (cells == null) return;
        foreach (var cell in cells)
        {
            if (cell == null || cell.Surface == null) continue;
            Gizmos.color = cell.ShadowSpawnable ? new Color(0.1f, 0.9f, 1f, 0.8f)
                : cell.ShadowWalkable ? new Color(0.2f, 0.5f, 1f, 0.7f)
                : cell.PlayerAccessible ? new Color(0.2f, 1f, 0.4f, 0.7f)
                : new Color(1f, 0.3f, 0.3f, 0.7f);
            Gizmos.DrawWireCube(cell.SurfacePoint, new Vector3(cellPitch * 0.9f, 0.03f, cellPitch * 0.9f));
        }
    }
}
