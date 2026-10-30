using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// A short grid route through the existing floor and wall Tilemaps.
// Guards use it only when displaced or when returning someone to the ward.
public static class LabPathfinder
{
    private static readonly Vector3Int[] Steps =
        { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    public static bool TryFind(Vector2 start, Vector2 goal, List<Vector2> route)
    {
        route.Clear();
        GameObject laboratory = GameObject.Find("Refined Laboratory");
        if (laboratory == null) return false;

        Transform root = laboratory.transform;
        Tilemap floor = root.Find("01 Floor").GetComponent<Tilemap>();
        Tilemap walls = root.Find("06 Wall collision").GetComponent<Tilemap>();
        Vector3Int from = NearestOpen(floor, walls, ToCell(floor, start));
        Vector3Int to = NearestOpen(floor, walls, ToCell(floor, goal));
        if (!IsOpen(floor, walls, from) || !IsOpen(floor, walls, to)) return false;

        var queue = new Queue<Vector3Int>();
        var previous = new Dictionary<Vector3Int, Vector3Int>();
        queue.Enqueue(from);
        previous[from] = from;

        while (queue.Count > 0 && !previous.ContainsKey(to))
        {
            Vector3Int current = queue.Dequeue();
            foreach (Vector3Int step in Steps)
            {
                Vector3Int next = current + step;
                if (previous.ContainsKey(next) || !IsOpen(floor, walls, next)) continue;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }
        if (!previous.ContainsKey(to)) return false;

        var cells = new List<Vector3Int>();
        for (Vector3Int cell = to; cell != from; cell = previous[cell])
            cells.Add(cell);
        cells.Add(from);
        cells.Reverse();

        route.Add(floor.GetCellCenterWorld(cells[0]));
        for (int i = 1; i < cells.Count - 1; i++)
        {
            if (cells[i] - cells[i - 1] != cells[i + 1] - cells[i])
                route.Add(floor.GetCellCenterWorld(cells[i]));
        }
        if (cells.Count > 1) route.Add(floor.GetCellCenterWorld(cells[cells.Count - 1]));
        if (Vector2.Distance(route[route.Count - 1], goal) > 0.1f) route.Add(goal);
        return true;
    }

    private static Vector3Int ToCell(Tilemap floor, Vector2 point)
    {
        Vector3Int cell = floor.WorldToCell(new Vector3(point.x, point.y, floor.transform.position.z));
        cell.z = 0;
        return cell;
    }

    private static Vector3Int NearestOpen(Tilemap floor, Tilemap walls, Vector3Int source)
    {
        if (IsOpen(floor, walls, source)) return source;
        for (int radius = 1; radius <= 4; radius++)
        for (int x = -radius; x <= radius; x++)
        for (int y = -radius; y <= radius; y++)
        {
            if (Mathf.Abs(x) + Mathf.Abs(y) != radius) continue;
            Vector3Int candidate = source + new Vector3Int(x, y, 0);
            if (IsOpen(floor, walls, candidate)) return candidate;
        }
        return source;
    }

    private static bool IsOpen(Tilemap floor, Tilemap walls, Vector3Int cell) =>
        floor.HasTile(cell) && !walls.HasTile(cell);
}
