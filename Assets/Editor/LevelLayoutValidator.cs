using System.Collections.Generic;
using UnityEngine;

public static class LevelLayoutValidator
{
    static readonly Vector2Int[] Steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    public static List<string> Validate(string[] rows, int width, int height, int totalReds)
    {
        var errors = new List<string>();
        if (rows == null || rows.Length != height)
        {
            errors.Add("Layout must have " + height + " rows.");
            return errors;
        }

        for (var y = 0; y < height; y++)
        {
            if (rows[y].Length != width)
                errors.Add("Row " + y + " must be " + width + " wide (is " + rows[y].Length + ").");
        }

        if (errors.Count > 0)
            return errors;

        var spawns = new List<Vector2Int>();
        var reds = new List<Vector2Int>();
        var exits = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var c = rows[y][x];
                if (c == 'P')
                    spawns.Add(new Vector2Int(x, y));
                else if (c == 'r')
                    reds.Add(new Vector2Int(x, y));
                else if (c == 'E')
                    exits++;
                else if (c != '#' && c != '.' && c != 'd')
                    errors.Add("Unknown tile '" + c + "' at " + x + "," + y + ".");

                var border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                if (border && c != '#' && c != 'E')
                    errors.Add("Border must be stone at " + x + "," + y + ".");
            }
        }

        if (spawns.Count != 1)
            errors.Add("Layout needs exactly one P (has " + spawns.Count + ").");
        if (exits != 2 || !rows[0].Contains("EE"))
            errors.Add("Layout needs one EE door in the top row.");
        if (reds.Count != totalReds)
            errors.Add("Layout needs " + totalReds + " reds (has " + reds.Count + ").");
        if (errors.Count > 0)
            return errors;

        var withDig = Flood(rows, spawns[0], true);
        var withoutDig = Flood(rows, spawns[0], false);
        foreach (var red in reds)
        {
            if (!withDig.Contains(red))
                errors.Add("Red at " + red + " is unreachable.");
            if (withoutDig.Contains(red))
                errors.Add("Red at " + red + " is reachable without digging.");
        }

        for (var x = 0; x < width; x++)
        {
            if (rows[0][x] == 'E' && !withDig.Contains(new Vector2Int(x, 0)))
                errors.Add("Exit at " + x + ",0 is unreachable.");
        }

        return errors;
    }

    static HashSet<Vector2Int> Flood(string[] rows, Vector2Int start, bool passDirt)
    {
        var seen = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var step in Steps)
            {
                var next = cell + step;
                if (next.y < 0 || next.y >= rows.Length || next.x < 0 || next.x >= rows[next.y].Length)
                    continue;
                var c = rows[next.y][next.x];
                if (c == '#' || (c == 'd' && !passDirt) || !seen.Add(next))
                    continue;
                if (c != 'E')
                    queue.Enqueue(next);
            }
        }

        return seen;
    }
}
