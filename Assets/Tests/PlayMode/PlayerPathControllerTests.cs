using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PlayerPathControllerTests
{
    [UnityTest]
    public IEnumerator Stage01_player_moves_on_edges_and_rejects_input_while_traveling()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(graph, Is.Not.Null);
        Assert.That(player.GetComponent<PathMover>(), Is.Null);
        Assert.That(graph.Nodes.Length, Is.EqualTo(18));

        Assert.That(controller.TryMoveTo(graph.Nodes[1]), Is.True);
        Assert.That(controller.IsMoving, Is.True);
        Assert.That(controller.TryMoveTo(graph.Nodes[2]), Is.False);
        yield return new WaitForSeconds(0.25f);
        Assert.That(player.transform.position.x, Is.GreaterThan(-9f).And.LessThan(-7.5f));
        Assert.That(player.transform.position.z, Is.EqualTo(0f).Within(0.03f));
        Assert.That(Vector3.Dot(player.transform.forward, Vector3.right), Is.GreaterThan(0.6f));

        var deadline = Time.time + 2f;
        while (controller.IsMoving && Time.time < deadline)
        {
            yield return null;
        }
        var nearby = Physics.OverlapSphere(player.transform.position + Vector3.up, 2f,
            ~0, QueryTriggerInteraction.Ignore);
        Assert.That(controller.IsMoving, Is.False,
            $"Route did not finish: player={player.transform.position}, target={graph.Nodes[1].Position}, current={controller.CurrentNode}; nearby="
                + string.Join(", ", System.Array.ConvertAll(nearby, item => item.name + ":" + item.bounds)));
        Assert.That(controller.CurrentNode, Is.SameAs(graph.Nodes[1]));
        Assert.That(Vector3.Distance(player.transform.position, graph.Nodes[1].Position), Is.LessThan(0.03f));
    }

    [UnityTest]
    public IEnumerator Bridge_and_gate_conditions_change_reachability()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var graph = Object.FindFirstObjectByType<PathGraph>();
        var camera = Object.FindFirstObjectByType<AngleSnapCamera>();
        var gate = Object.FindFirstObjectByType<StageGate>();
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, graph.Nodes[0], graph.Nodes[8]), Is.Null);

        camera.SetAnchorIndexForTest(0);
        yield return new WaitForSeconds(0.25f);
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, graph.Nodes[0], graph.Nodes[8]), Is.Not.Null);
        Assert.That(Pathfinder.FindPath(graph, graph.Nodes[0], graph.Nodes[17]), Is.Null);

        gate.Open();
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, graph.Nodes[0], graph.Nodes[17]), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator Checkpoint_restore_snaps_player_back_to_a_graph_node()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        var service = Object.FindFirstObjectByType<CheckpointService>();
        var marker = GameObject.Find("Stage01_T7_CheckpointAfterGate").GetComponent<CheckpointMarker>();
        service.Activate(marker);
        service.RestoreLatestCheckpoint();
        yield return null;

        Assert.That(controller.IsMoving, Is.False);
        Assert.That(controller.CurrentNode, Is.SameAs(graph.Nodes[14]));
        Assert.That(Vector3.Distance(player.transform.position, graph.Nodes[14].Position), Is.LessThan(0.02f));
    }

    [UnityTest]
    public IEnumerator Closing_the_current_edge_stops_before_crossing_it()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        graph.Edges = new[]
        {
            new PathEdge { Id = "test-first", From = graph.Nodes[0], To = graph.Nodes[1] }
        };
        graph.Rebuild();
        Assert.That(controller.TryMoveTo(graph.Nodes[1]), Is.True);
        yield return new WaitForSeconds(0.1f);
        Assert.That(controller.IsMoving, Is.True);

        graph.SetEdgeEnabled("test-first", false);
        yield return null;
        Assert.That(controller.IsMoving, Is.False);
        Assert.That(controller.CurrentNode, Is.SameAs(graph.Nodes[0]));
        Assert.That(Vector3.Distance(player.transform.position, graph.Nodes[0].Position), Is.LessThan(0.02f));
    }

    [UnityTest]
    public IEnumerator Rotating_a_mechanism_node_refreshes_the_graph_automatically()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var graph = Object.FindFirstObjectByType<PathGraph>();
        var node = graph.Nodes[1];
        var originalParent = node.transform.parent;
        var originalPosition = node.Position;
        var pivot = new GameObject("TestRotationPivot");
        pivot.transform.position = new Vector3(-7f, 1f, 0f);
        node.transform.SetParent(pivot.transform, true);
        graph.Rebuild();
        Assert.That(graph.IsTraversable(graph.Nodes[0], node), Is.True);

        pivot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        yield return null;
        Assert.That(graph.IsTraversable(graph.Nodes[0], node), Is.False);

        node.transform.SetParent(originalParent, true);
        node.transform.position = originalPosition;
        Object.Destroy(pivot);
        yield return null;
        Assert.That(graph.IsTraversable(graph.Nodes[0], node), Is.True);
    }

    [UnityTest]
    public IEnumerator Physical_obstruction_releases_the_movement_lock()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        var wall = new GameObject("TestRouteObstacle");
        wall.transform.position = new Vector3(-7.5f, 1.5f, 0f);
        var collider = wall.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.3f, 3f, 3f);
        Assert.That(controller.TryMoveTo(graph.Nodes[1]), Is.True);

        yield return new WaitForSeconds(0.7f);
        Assert.That(controller.IsMoving, Is.False);
        Assert.That(controller.CurrentNode, Is.SameAs(graph.Nodes[0]));
        Assert.That(Vector3.Distance(player.transform.position, graph.Nodes[0].Position), Is.LessThan(0.02f));
        Object.Destroy(wall);
    }

    [UnityTest]
    public IEnumerator Idle_player_follows_a_rotating_current_node()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);

        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        var node = graph.Nodes[0];
        yield return null;
        Assert.That(controller.CurrentNode, Is.SameAs(node));
        var originalParent = node.transform.parent;
        var originalPosition = node.Position;
        var pivot = new GameObject("TestPlayerPlatformPivot");
        pivot.transform.position = new Vector3(-8.5f, 1f, 0f);
        node.transform.SetParent(pivot.transform, true);
        pivot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        yield return null;

        Assert.That(controller.CurrentNode, Is.SameAs(node));
        Assert.That(Vector3.Distance(player.transform.position, node.Position), Is.LessThan(0.02f));
        Assert.That(controller.IsMoving, Is.False);

        node.transform.SetParent(originalParent, true);
        node.transform.position = originalPosition;
        Object.Destroy(pivot);
    }

    [UnityTest]
    public IEnumerator Player_travel_event_reports_achieved_horizontal_distance()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);
        var player = GameObject.Find("Stage01_T7_Player");
        var controller = player.GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        var deltas = new System.Collections.Generic.List<Vector3>();
        controller.TravelDisplacement += deltas.Add;
        var start = player.transform.position;

        Assert.That(controller.TryMoveTo(graph.Nodes[1]), Is.True);
        var deadline = Time.time + 2f;
        while (controller.IsMoving && Time.time < deadline) yield return null;

        float emittedX = 0f;
        foreach (var delta in deltas)
        {
            Assert.That(Mathf.Abs(delta.y), Is.LessThan(0.0001f));
            emittedX += delta.x;
        }
        Assert.That(emittedX, Is.EqualTo(player.transform.position.x - start.x).Within(0.03f));
        Assert.That(emittedX, Is.GreaterThan(1.3f));
    }

    [UnityTest]
    public IEnumerator Checkpoint_snap_does_not_emit_travel_event()
    {
        var load = SceneManager.LoadSceneAsync("Stage01_OffsetBridge");
        yield return new WaitUntil(() => load.isDone);
        var controller = GameObject.Find("Stage01_T7_Player").GetComponent<PlayerPathController>();
        var graph = Object.FindFirstObjectByType<PathGraph>();
        var emitted = 0;
        controller.TravelDisplacement += _ => emitted++;

        Assert.That(controller.RestoreToNearestNode(graph.Nodes[14].Position, Quaternion.identity), Is.True);
        yield return null;

        Assert.That(emitted, Is.Zero);
    }
}
