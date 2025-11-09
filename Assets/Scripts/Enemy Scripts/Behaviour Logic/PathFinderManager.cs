using UnityEngine;
using System.Collections.Generic;

public class PathFinderManager : MonoBehaviour
{
    public Vector2Int gridSize;
    public float cellSize = 1f;
    public LayerMask obstacleMask;

    private HashSet<Vector2Int> blockedNodes = new HashSet<Vector2Int>();

    public List<Vector2> FindPath(Vector2 startWorldPos, Vector2 targetWorldPos)
    {
        Vector2Int start = WorldToGrid(startWorldPos);
        Vector2Int goal = WorldToGrid(targetWorldPos);

        var openSet = new PriorityQueue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();

        openSet.Enqueue(start, 0);
        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.Dequeue();
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (IsBlocked(neighbor)) continue;

                float tentativeG = gScore[current] + Vector2Int.Distance(current, neighbor);
                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return null;
    }

    private float Heuristic(Vector2Int a, Vector2Int b) => Vector2Int.Distance(a, b);

    private List<Vector2> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var totalPath = new List<Vector2> { GridToWorld(current) };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, GridToWorld(current));
        }
        return totalPath;
    }

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize)
        );
    }

    private Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
    }

    private bool IsBlocked(Vector2Int gridPos)
    {
        if (blockedNodes.Contains(gridPos)) return true;
        Vector2 world = GridToWorld(gridPos);
        if (Physics2D.OverlapBox(world, Vector2.one * (cellSize * 0.8f), 0, obstacleMask))
        {
            blockedNodes.Add(gridPos);
            return true;
        }
        return false;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int node)
    {
        List<Vector2Int> neighbors = new();
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int next = node + dir;
            if (Mathf.Abs(next.x) < gridSize.x / 2 && Mathf.Abs(next.y) < gridSize.y / 2)
                neighbors.Add(next);
        }

        return neighbors;
    }
}

public class PriorityQueue<T>
{
    private List<(T item, float priority)> elements = new();
    public int Count => elements.Count;

    public void Enqueue(T item, float priority)
    {
        elements.Add((item, priority));
    }

    public T Dequeue()
    {
        int bestIndex = 0;
        for (int i = 1; i < elements.Count; i++)
            if (elements[i].priority < elements[bestIndex].priority)
                bestIndex = i;

        T bestItem = elements[bestIndex].item;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}
