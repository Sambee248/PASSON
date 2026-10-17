using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class GridBoardTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var item in created)
        {
            if (item != null) Object.DestroyImmediate(item);
        }
        created.Clear();
    }

    [Test]
    public void Four_neighbor_lookup_does_not_join_diagonal_tiles()
    {
        var origin = Cell(0, 0, true, true);
        var east = Cell(1, 0, true, true);
        var diagonal = Cell(1, 1, true, true);
        var board = Board(origin, east, diagonal);

        Assert.That(board.TryGetCell(new Vector2Int(1, 0), out var indexed), Is.True);
        Assert.That(indexed, Is.SameAs(east));
        Assert.That(board.GetOrthogonalNeighbors(origin), Has.Member(east));
        Assert.That(board.GetOrthogonalNeighbors(origin), Has.No.Member(diagonal));
    }

    [Test]
    public void Decorative_collider_is_not_a_cell_and_missing_tile_has_no_shadow_support()
    {
        var origin = Cell(0, 0, true, true);
        var board = Board(origin);
        var decoration = GameObject.CreatePrimitive(PrimitiveType.Cube);
        created.Add(decoration);
        decoration.transform.position = new Vector3(3f, 0.9f, 0f);
        Physics.SyncTransforms();

        Assert.That(board.TryGetCell(decoration.GetComponent<Collider>(), out _), Is.False);
        Assert.That(board.TryGetCellAtWorld(new Vector3(5f, 1f, 0f), out _), Is.False);
        Assert.That(board.CanShadowOccupy(new Vector3(0f, 1f, 0f), 0.35f), Is.True);
        Assert.That(board.CanShadowOccupy(new Vector3(-0.7f, 1f, 0f), 0.35f), Is.False);
    }

    private GridCell Cell(int x, int z, bool player, bool shadow)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = $"Cell_{x}_{z}";
        tile.layer = LayerMask.NameToLayer("Walkable");
        tile.transform.position = new Vector3(x * 1.5f, 0.9f, z * 1.5f);
        tile.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
        created.Add(tile);
        PathNode node = null;
        if (player)
        {
            var nodeObject = new GameObject("Node");
            nodeObject.transform.SetParent(tile.transform, false);
            nodeObject.transform.position = tile.transform.position + Vector3.up * 0.1f;
            node = nodeObject.AddComponent<PathNode>();
        }
        var cell = tile.AddComponent<GridCell>();
        cell.Configure(new Vector2Int(x, z), player, shadow, shadow,
            tile.GetComponent<Collider>(), node, null, true);
        return cell;
    }

    private GridBoard Board(params GridCell[] cells)
    {
        var boardObject = new GameObject("Board");
        created.Add(boardObject);
        var board = boardObject.AddComponent<GridBoard>();
        board.Configure(1.5f, cells);
        Physics.SyncTransforms();
        return board;
    }
}
