using System.Collections.Generic;
using UnityEngine;

public sealed class ShadowPlacementValidator
{
    private readonly GridBoard board;
    private readonly Transform player;
    private readonly float placementRadius;
    private readonly float capsuleRadius;
    private readonly float capsuleHeight;
    private readonly LayerMask clearanceMask;
    private readonly LayerMask walkableMask;
    private readonly float cursorRayDistance;
    private readonly List<GridCell> eligibleCells = new List<GridCell>();

    public ShadowPlacementValidator(GridBoard board, Transform player, float placementRadius,
        float capsuleRadius, float capsuleHeight, LayerMask clearanceMask,
        LayerMask walkableMask, float cursorRayDistance)
    {
        this.board = board;
        this.player = player;
        this.placementRadius = placementRadius;
        this.capsuleRadius = capsuleRadius;
        this.capsuleHeight = capsuleHeight;
        this.clearanceMask = clearanceMask;
        this.walkableMask = walkableMask;
        this.cursorRayDistance = cursorRayDistance;
    }

    public bool IsEligible(GridCell cell)
    {
        if (board == null || player == null || cell == null || !cell.ShadowSpawnable
            || cell.Surface == null || !board.TryGetCell(cell.Coordinate, out var indexed)
            || indexed != cell)
        {
            return false;
        }

        var point = cell.SurfacePoint;
        if ((walkableMask.value & (1 << cell.Surface.gameObject.layer)) == 0
            || !ShadowPlacement.IsInsideRange(player.position, point, placementRadius)
            || !board.CanShadowOccupy(point, capsuleRadius))
        {
            return false;
        }

        var height = Mathf.Max(capsuleHeight, capsuleRadius * 2f);
        var overlaps = Physics.OverlapCapsule(point + Vector3.up * capsuleRadius,
            point + Vector3.up * (height - capsuleRadius), capsuleRadius,
            clearanceMask, QueryTriggerInteraction.Ignore);
        foreach (var collider in overlaps)
        {
            if (collider != cell.Surface) return false;
        }
        return true;
    }

    public IReadOnlyList<GridCell> GetEligibleCells()
    {
        eligibleCells.Clear();
        if (board == null) return eligibleCells;
        foreach (var cell in board.Cells)
        {
            if (IsEligible(cell)) eligibleCells.Add(cell);
        }
        return eligibleCells;
    }

    public bool TryResolveCursor(Ray ray, out GridCell cell)
    {
        cell = null;
        return board != null
            && Physics.Raycast(ray, out var hit, cursorRayDistance, walkableMask,
                QueryTriggerInteraction.Ignore)
            && board.TryGetCell(hit.collider, out cell)
            && IsEligible(cell);
    }
}
