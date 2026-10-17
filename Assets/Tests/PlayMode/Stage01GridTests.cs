using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Stage01GridTests
{
    [UnityTest]
    public IEnumerator First_stage_uses_contiguous_tiles_and_a_separate_shadow_lane()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var board = Object.FindFirstObjectByType<GridBoard>();
        Assert.That(board, Is.Not.Null);
        Assert.That(board.CellPitch, Is.EqualTo(1.5f).Within(0.01f));
        for (var x = 0; x <= 17; x++)
        {
            Assert.That(board.TryGetCell(new Vector2Int(x, 0), out var cell), Is.True,
                $"Missing player tile {x}");
            Assert.That(cell.PlayerAccessible || x == 6 || x == 12, Is.True);
            Assert.That(cell.Node, Is.Not.Null);
            Assert.That(cell.ShadowSpawnable, Is.False);
        }
        Assert.That(board.TryGetCell(new Vector2Int(8, 2), out var spawn), Is.True);
        Assert.That(spawn.ShadowSpawnable, Is.True);
        Assert.That(board.TryGetCell(new Vector2Int(10, 2), out var plate), Is.True);
        Assert.That(plate.ShadowWalkable, Is.True);
        Assert.That(plate.ShadowSpawnable, Is.False);
        Assert.That(board.TryGetCell(new Vector2Int(8, 1), out _), Is.False,
            "The remote shadow route must be distinct from the player lane.");
    }

    [UnityTest]
    public IEnumerator Bridge_and_gate_block_grid_route_until_their_mechanisms_activate()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var board = Object.FindFirstObjectByType<GridBoard>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        Assert.That(board, Is.Not.Null);
        board.TryGetCell(new Vector2Int(0, 0), out var start);
        board.TryGetCell(new Vector2Int(11, 0), out var beforeGate);
        board.TryGetCell(new Vector2Int(17, 0), out var finish);
        Assert.That(Pathfinder.FindPath(graph, start.Node, beforeGate.Node), Is.Null);

        Object.FindFirstObjectByType<AngleSnapCamera>().SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.25f);
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, start.Node, beforeGate.Node), Is.Not.Null);
        Assert.That(Pathfinder.FindPath(graph, start.Node, finish.Node), Is.Null);

        Object.FindFirstObjectByType<StageGate>().Open();
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, start.Node, finish.Node), Is.Not.Null);
    }
}
