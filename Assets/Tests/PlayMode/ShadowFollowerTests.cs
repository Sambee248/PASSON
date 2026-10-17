using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ShadowFollowerTests
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

    [UnityTest]
    public IEnumerator Shadow_copies_player_travel_across_supported_cells()
    {
        var board = Board(0, 1, 2);
        var follower = Follower(board);
        yield return null;

        var achieved = follower.ApplyDisplacement(Vector3.right * board.CellPitch);

        Assert.That(achieved.x, Is.EqualTo(board.CellPitch).Within(0.04f));
        Assert.That(follower.transform.position.x, Is.EqualTo(board.CellPitch).Within(0.04f));
    }

    [UnityTest]
    public IEnumerator Swept_follow_stops_before_missing_cell_and_never_catches_up()
    {
        var board = Board(0, 2);
        var follower = Follower(board);
        yield return null;

        var achieved = follower.ApplyDisplacement(Vector3.right * board.CellPitch * 2f);
        var stoppedX = follower.transform.position.x;
        Assert.That(achieved.x, Is.GreaterThan(0f).And.LessThan(board.CellPitch));
        Assert.That(stoppedX, Is.LessThan(board.CellPitch * 0.5f));

        yield return null;
        Assert.That(follower.transform.position.x, Is.EqualTo(stoppedX).Within(0.01f));
    }

    [UnityTest]
    public IEnumerator Physical_obstruction_stops_shadow_without_deferred_motion()
    {
        var board = Board(0, 1, 2);
        var follower = Follower(board);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        created.Add(wall);
        wall.transform.position = new Vector3(0.85f, 1.9f, 0f);
        wall.transform.localScale = new Vector3(0.15f, 1.8f, 1.5f);
        Physics.SyncTransforms();
        yield return null;

        var achieved = follower.ApplyDisplacement(Vector3.right * board.CellPitch);
        Assert.That(achieved.x, Is.LessThan(board.CellPitch * 0.7f));
        var stoppedX = follower.transform.position.x;
        Object.DestroyImmediate(wall);
        yield return null;
        Assert.That(follower.transform.position.x, Is.EqualTo(stoppedX).Within(0.01f));
    }

    private GridBoard Board(params int[] xCoordinates)
    {
        var cells = new List<GridCell>();
        foreach (var x in xCoordinates)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            created.Add(tile);
            tile.name = $"ShadowTile_{x}";
            tile.layer = LayerMask.NameToLayer("Walkable");
            tile.transform.position = new Vector3(x * 1.5f, 0.9f, 0f);
            tile.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
            var cell = tile.AddComponent<GridCell>();
            cell.Configure(new Vector2Int(x, 0), false, true, false,
                tile.GetComponent<Collider>(), null, null, true);
            cells.Add(cell);
        }
        var boardObject = new GameObject("TestBoard");
        created.Add(boardObject);
        var board = boardObject.AddComponent<GridBoard>();
        board.Configure(1.5f, cells.ToArray());
        Physics.SyncTransforms();
        return board;
    }

    private ShadowFollower Follower(GridBoard board)
    {
        var shadow = new GameObject("TestShadow");
        created.Add(shadow);
        shadow.layer = LayerMask.NameToLayer("Shadow");
        shadow.transform.position = new Vector3(0f, 1f, 0f);
        var capsule = shadow.AddComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.radius = 0.35f;
        capsule.center = Vector3.up * 0.9f;
        capsule.minMoveDistance = 0f;
        var follower = shadow.AddComponent<ShadowFollower>();
        follower.Configure(board);
        Physics.SyncTransforms();
        return follower;
    }
}
