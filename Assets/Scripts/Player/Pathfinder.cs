using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    public static List<PathNode> FindPath(PathGraph graph, PathNode start, PathNode goal)
    {
        if (graph == null || !graph.Contains(start) || !graph.Contains(goal))
        {
            return null;
        }
        if (start == goal)
        {
            return new List<PathNode> { start };
        }

        var open = new List<PathNode> { start };
        var closed = new HashSet<PathNode>();
        var previous = new Dictionary<PathNode, PathNode>();
        var cost = new Dictionary<PathNode, float> { [start] = 0f };

        while (open.Count > 0)
        {
            var bestIndex = 0;
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < open.Count; index++)
            {
                var candidate = open[index];
                var score = cost[candidate] + Vector3.Distance(candidate.Position, goal.Position);
                if (score < bestScore)
                {
                    bestIndex = index;
                    bestScore = score;
                }
            }

            var current = open[bestIndex];
            open.RemoveAt(bestIndex);
            if (current == goal)
            {
                var route = new List<PathNode> { goal };
                while (previous.TryGetValue(route[route.Count - 1], out var parent))
                {
                    route.Add(parent);
                }
                route.Reverse();
                return route;
            }

            closed.Add(current);
            foreach (var neighbor in graph.GetOutgoing(current))
            {
                if (closed.Contains(neighbor.Node))
                {
                    continue;
                }
                var nextCost = cost[current] + neighbor.Cost;
                if (!cost.TryGetValue(neighbor.Node, out var knownCost) || nextCost < knownCost)
                {
                    cost[neighbor.Node] = nextCost;
                    previous[neighbor.Node] = current;
                    if (!open.Contains(neighbor.Node))
                    {
                        open.Add(neighbor.Node);
                    }
                }
            }
        }
        return null;
    }
}
