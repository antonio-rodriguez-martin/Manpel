using System;
using System.Collections.Generic;
using Godot;

public static class Pathfinder
{
    public static Vector2I FindNextStep(
        Vector2I start,
        Vector2I goal,
        Func<Vector2I, bool> isBlocked)
    {
        if (start == goal) return Vector2I.Zero;

        var cameFrom = new Dictionary<Vector2I, Vector2I> { [start] = start };
        var queue = new Queue<Vector2I>();
        queue.Enqueue(start);

        // ADD THIS: Limit how many tiles the enemy is allowed to search
        int maxNodesToSearch = 500; 
        int nodesSearched = 0;

        while (queue.Count > 0)
        {
            // ADD THIS: If it searches too much, stop to prevent a memory leak
            if (nodesSearched++ > maxNodesToSearch) 
                return Vector2I.Zero; 

            var cur = queue.Dequeue();

            foreach (var d in GridMath.CardinalDirs)
            {
                var nxt = cur + d;
                if (cameFrom.ContainsKey(nxt)) continue;

                if (nxt != goal && isBlocked(nxt)) continue;

                cameFrom[nxt] = cur;
                if (nxt == goal) goto found;
                queue.Enqueue(nxt);
            }
        }
        return Vector2I.Zero; // No se llega

    found:
        var step = goal;
        while (cameFrom[step] != start) step = cameFrom[step];
        return step - start;
    }
}