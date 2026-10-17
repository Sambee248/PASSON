using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ShadowPlacementPreviewTests
{
    private readonly List<GameObject> created = new List<GameObject>();
    private ShadowController shadowController;

    [TearDown]
    public void TearDown()
    {
        if (shadowController != null) shadowController.Recall();
        foreach (var item in created)
        {
            if (item != null) Object.DestroyImmediate(item);
        }
        created.Clear();
    }

    [UnityTest]
    public IEnumerator Hold_shows_only_eligible_noncolliding_dots_and_release_commits_hover()
    {
        var player = Actor();
        var valid = Tile(0, 4f, false, true, true);
        var second = Tile(1, 5.5f, false, true, true);
        var plate = Tile(2, 7f, false, true, false);
        var board = Board(valid, second, plate);
        var placement = Placement(player, board);
        Physics.SyncTransforms();
        yield return null;

        Assert.That(placement.BeginPreview(), Is.True);
        Assert.That(placement.Preview.VisibleCount, Is.EqualTo(2));
        Assert.That(placement.Preview.GetComponentsInChildren<Collider>(), Is.Empty);
        placement.UpdatePreview(RayAbove(valid));
        Assert.That(placement.Preview.HoveredCell, Is.SameAs(valid));
        Assert.That(placement.EndPreview(), Is.True);
        Assert.That(placement.Preview.VisibleCount, Is.Zero);
        Assert.That(shadowController.ActiveShadow, Is.Not.Null);
        yield return new WaitForSeconds(0.4f);
        Assert.That(shadowController.ActiveShadow.transform.position.x,
            Is.EqualTo(valid.SurfacePoint.x).Within(0.02f));

        var previous = shadowController.ActiveShadow;
        Assert.That(placement.BeginPreview(), Is.True);
        placement.UpdatePreview(RayAbove(plate));
        Assert.That(placement.Preview.HoveredCell, Is.Null);
        Assert.That(placement.EndPreview(), Is.False);
        Assert.That(shadowController.ActiveShadow, Is.SameAs(previous));
    }

    [UnityTest]
    public IEnumerator Moving_player_cannot_start_or_commit_a_preview()
    {
        var player = Actor();
        var start = Tile(0, 0f, true, false, false);
        var next = Tile(1, 1.5f, true, false, false);
        var valid = Tile(2, 4f, false, true, true);
        var board = Board(start, next, valid);
        var graphObject = new GameObject("Graph");
        created.Add(graphObject);
        var graph = graphObject.AddComponent<PathGraph>();
        graph.Board = board;
        graph.Rebuild();
        var mover = player.AddComponent<PlayerPathController>();
        typeof(PlayerPathController).GetField("graph",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(mover, graph);
        var placement = Placement(player, board);
        Physics.SyncTransforms();
        yield return null;

        Assert.That(mover.CurrentNode, Is.SameAs(start.Node),
            $"player={player.transform.position}, start={start.Node.Position}, graphContains={graph.Contains(start.Node)}");
        Assert.That(Vector3.Distance(player.transform.position, start.Node.Position), Is.LessThan(0.1f));
        Assert.That(graph.IsTraversable(start.Node, next.Node), Is.True,
            $"start={start.Node.Position}, next={next.Node.Position}, pitch={board.CellPitch}");
        Assert.That(mover.TryMoveTo(next.Node), Is.True);
        Assert.That(placement.BeginPreview(), Is.False);
        Assert.That(placement.Preview.VisibleCount, Is.Zero);
        Assert.That(shadowController.ActiveShadow, Is.Null);
        yield return new WaitUntil(() => !mover.IsMoving);

        Assert.That(placement.BeginPreview(), Is.True);
        placement.UpdatePreview(RayAbove(valid));
        Assert.That(placement.Preview.HoveredCell, Is.SameAs(valid));
        Assert.That(mover.TryMoveTo(start.Node), Is.True);
        Assert.That(placement.EndPreview(), Is.False);
        Assert.That(shadowController.ActiveShadow, Is.Null);
        Assert.That(placement.Preview.VisibleCount, Is.Zero);
    }

    private GameObject Actor()
    {
        var actor = new GameObject("Player");
        created.Add(actor);
        actor.transform.position = new Vector3(1000f, 1f, 1000f);
        var capsule = actor.AddComponent<CharacterController>();
        capsule.center = Vector3.up;
        return actor;
    }

    private GridCell Tile(int x, float offset, bool player, bool shadow, bool spawn)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        created.Add(tile);
        tile.layer = LayerMask.NameToLayer("Walkable");
        tile.transform.position = new Vector3(1000f + offset, 0.9f, 1000f);
        tile.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
        var cell = tile.AddComponent<GridCell>();
        PathNode node = null;
        if (player)
        {
            var marker = new GameObject("Node");
            marker.transform.SetParent(tile.transform, false);
            marker.transform.position = new Vector3(tile.transform.position.x,
                1f, tile.transform.position.z);
            node = marker.AddComponent<PathNode>();
        }
        cell.Configure(new Vector2Int(x, 0), player, shadow, spawn,
            tile.GetComponent<Collider>(), node, null, true);
        return cell;
    }

    private GridBoard Board(params GridCell[] cells)
    {
        var root = new GameObject("GridBoard");
        created.Add(root);
        var board = root.AddComponent<GridBoard>();
        board.Configure(1.5f, cells);
        return board;
    }

    private ShadowPlacement Placement(GameObject player, GridBoard board)
    {
        shadowController = player.AddComponent<ShadowController>();
        var prefab = new GameObject("ShadowPrefab");
        created.Add(prefab);
        prefab.AddComponent<CharacterController>();
        prefab.AddComponent<ShadowFollower>().Configure(board);
        typeof(ShadowController).GetField("shadowPrefab",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(shadowController, prefab);
        var placement = player.AddComponent<ShadowPlacement>();
        placement.Configure(board, shadowController);
        return placement;
    }

    private static Ray RayAbove(GridCell cell) =>
        new Ray(cell.SurfacePoint + Vector3.up * 4f, Vector3.down);
}
