using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class PathNavigationTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var item in objects)
        {
            Object.DestroyImmediate(item);
        }
        objects.Clear();
    }

    [Test]
    public void One_way_edge_rejects_reverse_route()
    {
        var a = Node("A", Vector3.zero);
        var b = Node("B", Vector3.right);
        var graph = Graph(new[] { a, b }, new PathEdge
        {
            From = a, To = b, Enabled = true, Bidirectional = false
        });

        Assert.That(Pathfinder.FindPath(graph, a, b), Is.EqualTo(new[] { a, b }));
        Assert.That(Pathfinder.FindPath(graph, b, a), Is.Null);
    }

    [Test]
    public void Bidirectional_edge_allows_reverse_route()
    {
        var a = Node("A", Vector3.zero);
        var b = Node("B", Vector3.right);
        var graph = Graph(new[] { a, b }, new PathEdge { From = a, To = b, Enabled = true, Bidirectional = true });

        Assert.That(Pathfinder.FindPath(graph, b, a), Is.EqualTo(new[] { b, a }));
    }

    [Test]
    public void A_star_chooses_shorter_world_route()
    {
        var a = Node("A", Vector3.zero);
        var b = Node("B", new Vector3(1, 0, 0));
        var c = Node("C", new Vector3(0, 0, 10));
        var d = Node("D", new Vector3(2, 0, 0));
        var graph = Graph(new[] { a, b, c, d },
            new PathEdge { From = a, To = b, Enabled = true },
            new PathEdge { From = b, To = d, Enabled = true },
            new PathEdge { From = a, To = c, Enabled = true },
            new PathEdge { From = c, To = d, Enabled = true });

        Assert.That(Pathfinder.FindPath(graph, a, d), Is.EqualTo(new[] { a, b, d }));
    }

    [Test]
    public void Collider_condition_and_manual_toggle_update_connectivity()
    {
        var a = Node("A", Vector3.zero);
        var b = Node("B", Vector3.right);
        var blocker = NewObject("Bridge").AddComponent<BoxCollider>();
        var edge = new PathEdge
        {
            Id = "bridge", From = a, To = b, Enabled = true,
            RequiredCollider = blocker, RequiredColliderEnabled = true
        };
        var graph = Graph(new[] { a, b }, edge);
        blocker.enabled = false;
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, a, b), Is.Null);

        blocker.enabled = true;
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, a, b), Is.Not.Null);
        Assert.That(graph.SetEdgeEnabled("bridge", false), Is.True);
        Assert.That(Pathfinder.FindPath(graph, a, b), Is.Null);
    }

    [Test]
    public void Nearest_node_respects_snap_limit()
    {
        var a = Node("A", Vector3.zero);
        var graph = Graph(new[] { a });

        Assert.That(graph.FindNearestNode(new Vector3(0.3f, 0, 0), 0.5f), Is.SameAs(a));
        Assert.That(graph.FindNearestNode(new Vector3(3f, 0, 0), 0.5f), Is.Null);
    }

    [Test]
    public void Moving_a_rotating_node_rebuilds_distance_gated_connection()
    {
        var a = Node("A", Vector3.zero);
        var b = Node("B", Vector3.right);
        var pivot = NewObject("Pivot");
        pivot.transform.position = new Vector3(2f, 0f, 0f);
        b.transform.SetParent(pivot.transform, true);
        var graph = Graph(new[] { a, b }, new PathEdge
        {
            From = a, To = b, Enabled = true, Bidirectional = true,
            MaxConnectionDistance = 1.1f
        });

        Assert.That(Pathfinder.FindPath(graph, a, b), Is.Not.Null);
        pivot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, a, b), Is.Null);
    }

    [Test]
    public void Grid_edges_use_four_neighbors_and_respect_disabled_middle_cell()
    {
        var origin = Tile(0, 0);
        var east = Tile(1, 0);
        var diagonal = Tile(1, 1);
        var condition = NewObject("BridgeCondition").AddComponent<BoxCollider>();
        east.Configure(east.Coordinate, true, false, false, east.Surface, east.Node,
            condition, true);
        var board = NewObject("Board").AddComponent<GridBoard>();
        board.Configure(1.5f, new[] { origin, east, diagonal });
        var graph = Graph(System.Array.Empty<PathNode>());
        graph.Board = board;
        graph.Rebuild();

        Assert.That(Pathfinder.FindPath(graph, origin.Node, diagonal.Node),
            Is.EqualTo(new[] { origin.Node, east.Node, diagonal.Node }));
        condition.enabled = false;
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, origin.Node, diagonal.Node), Is.Null);
    }

    [Test]
    public void Grid_graph_does_not_map_decorative_collider_to_a_destination()
    {
        var tile = Tile(0, 0);
        var decoration = NewObject("Decoration").AddComponent<BoxCollider>();
        var board = NewObject("Board").AddComponent<GridBoard>();
        board.Configure(1.5f, new[] { tile });
        var graph = Graph(System.Array.Empty<PathNode>());
        graph.Board = board;
        graph.Rebuild();

        Assert.That(graph.FindNodeForCollider(tile.Surface), Is.SameAs(tile.Node));
        Assert.That(graph.FindNodeForCollider(decoration), Is.Null);
    }

    [Test]
    public void Conditional_manual_edge_suppresses_automatic_edge_between_same_tiles()
    {
        var left = Tile(0, 0);
        var right = Tile(1, 0);
        var condition = NewObject("GateBlocker").AddComponent<BoxCollider>();
        var board = NewObject("Board").AddComponent<GridBoard>();
        board.Configure(1.5f, new[] { left, right });
        var edge = new PathEdge
        {
            From = left.Node, To = right.Node, Enabled = true, Bidirectional = true,
            RequiredCollider = condition, RequiredColliderEnabled = false
        };
        var graph = Graph(System.Array.Empty<PathNode>(), edge);
        graph.Board = board;
        graph.Rebuild();

        Assert.That(Pathfinder.FindPath(graph, left.Node, right.Node), Is.Null);
        condition.enabled = false;
        graph.Rebuild();
        Assert.That(Pathfinder.FindPath(graph, left.Node, right.Node),
            Is.EqualTo(new[] { left.Node, right.Node }));
    }

    private GridCell Tile(int x, int z)
    {
        var tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tileObject.name = $"Cell_{x}_{z}";
        objects.Add(tileObject);
        tileObject.transform.position = new Vector3(x * 1.5f, 0.9f, z * 1.5f);
        tileObject.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
        var nodeObject = new GameObject("Node");
        nodeObject.transform.SetParent(tileObject.transform, false);
        nodeObject.transform.position = new Vector3(x * 1.5f, 1f, z * 1.5f);
        var node = nodeObject.AddComponent<PathNode>();
        var cell = tileObject.AddComponent<GridCell>();
        cell.Configure(new Vector2Int(x, z), true, false, false,
            tileObject.GetComponent<Collider>(), node, null, true);
        return cell;
    }

    private PathNode Node(string name, Vector3 position)
    {
        var go = NewObject(name);
        go.transform.position = position;
        return go.AddComponent<PathNode>();
    }

    private PathGraph Graph(PathNode[] nodes, params PathEdge[] edges)
    {
        var graph = NewObject("Graph").AddComponent<PathGraph>();
        graph.Nodes = nodes;
        graph.Edges = edges;
        graph.Rebuild();
        return graph;
    }

    private GameObject NewObject(string name)
    {
        var go = new GameObject(name);
        objects.Add(go);
        return go;
    }
}
