using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

public class MatchGroup
{
    public List<Vector2Int> TilePositions;
    public MatchShape Shape;
    public Petal Causer;
    
    /// <summary>
    /// From a skill combo swap. Resolver wont activate skill in this match nor animation
    /// </summary>
    public bool IsFromSkillCombo;
    
    public MatchGroup(List<Vector2Int> tilePositions, MatchShape shape, Petal causer = null, bool isFromSkillCombo = false)
    {
        TilePositions = tilePositions;
        Shape = shape;
        Causer = causer;
        IsFromSkillCombo = isFromSkillCombo;
    }
}

public static class MatchDetector
{
    public const int MinRunLength = 3;
    public const int MaxRunLength = 5;
    public static int GetMatchCheckRange() => MaxRunLength - 1;
    
    public static List<MatchGroup> Detect(Tile[,] grid)
    {
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);

        List<List<Vector2Int>> horizontalRuns = FindRuns(grid, cols, rows, horizontal: true);
        List<List<Vector2Int>> verticalRuns = FindRuns(grid, cols, rows, horizontal: false);

        HashSet<Vector2Int> consumed = new HashSet<Vector2Int>();
        List<MatchGroup> results = new List<MatchGroup>();

        // Detection priority: Five/Four > Cross > T/L > Square > Three.
        DetectLongRuns(horizontalRuns, consumed, results, minimumLength: 4);
        DetectLongRuns(verticalRuns, consumed, results, minimumLength: 4);
        DetectCross(horizontalRuns, verticalRuns, consumed, results);
        DetectTAndL(horizontalRuns, verticalRuns, consumed, results);
        DetectSquare2x2(grid, cols, rows, consumed, results);
        DetectLongRuns(horizontalRuns, consumed, results);
        DetectLongRuns(verticalRuns, consumed, results);
        return results;
    }
    
    //TODO: optimize this later, detecting on the whole grid is waisteful
    public static bool WouldCompleteMatch(Tile[,] grid, int x, int y)
    {
        List<MatchGroup> matches = Detect(grid);
        Vector2Int tile = new Vector2Int(x, y);
        return matches.Exists(g => g.TilePositions.Contains(tile));
    }
    
    private static List<List<Vector2Int>> FindRuns(Tile[,] grid, int cols, int rows, bool horizontal)
    {
        List<List<Vector2Int>> runs = new List<List<Vector2Int>>();

        int outer = horizontal ? rows : cols;
        int inner = horizontal ? cols : rows;

        for (int o = 0; o < outer; o++)
        {
            List<Vector2Int> current = new List<Vector2Int>();
            PetalType? lastType = null;

            for (int i = 0; i < inner; i++)
            {
                int x = horizontal ? i : o;
                int y = horizontal ? o : i;

                Tile tile = grid[x, y];

                if (tile != null && tile.IsMatchable())
                {
                    PetalType type = tile.Petal.PetalType;
                    if (type == lastType)
                    {
                        current.Add(new Vector2Int(x, y));
                    }
                    else
                    {
                        if (current.Count >= 3) runs.Add(new List<Vector2Int>(current));
                        current.Clear();
                        current.Add(new Vector2Int(x, y));
                        lastType = type;
                    }
                }
                else
                {
                    if (current.Count >= 3) runs.Add(new List<Vector2Int>(current));
                    current.Clear();
                    lastType = null;
                }
            }

            if (current.Count >= 3) runs.Add(new List<Vector2Int>(current));
        }

        return runs;
    }

    private static void DetectCross(List<List<Vector2Int>> hRuns, List<List<Vector2Int>> vRuns, HashSet<Vector2Int> consumed, List<MatchGroup> results)
    {
        foreach (var h in hRuns)
        {
            foreach (var v in vRuns)
            {
                List<Vector2Int> overlap = FindOverlap(h, v);
                if (overlap.Count != 1) continue;

                Vector2Int center = overlap[0];
                if (h.Count < 3 || v.Count < 3) continue;
                if (consumed.Contains(center)) continue;

                bool hEndpoint = center == h[0] || center == h[h.Count - 1];
                bool vEndpoint = center == v[0] || center == v[v.Count - 1];
                if (hEndpoint || vEndpoint) continue;

                HashSet<Vector2Int> tiles = new HashSet<Vector2Int>(h);
                foreach (var t in v) tiles.Add(t);

                if (tiles.Count >= 5)
                    AddGroup(new List<Vector2Int>(tiles), MatchShape.Cross, consumed, results);
            }
        }
    }

    private static void DetectTAndL(List<List<Vector2Int>> hRuns, List<List<Vector2Int>> vRuns, HashSet<Vector2Int> consumed, List<MatchGroup> results)
    {
        foreach (var h in hRuns)
        {
            foreach (var v in vRuns)
            {
                List<Vector2Int> overlap = FindOverlap(h, v);
                if (overlap.Count != 1) continue;

                Vector2Int junction = overlap[0];
                if (consumed.Contains(junction)) continue;

                bool hEndpoint = junction == h[0] || junction == h[h.Count - 1];
                bool vEndpoint = junction == v[0] || junction == v[v.Count - 1];

                if (!hEndpoint && !vEndpoint) continue;

                MatchShape shape = hEndpoint && vEndpoint
                    ? MatchShape.LShape
                    : MatchShape.TShape;

                HashSet<Vector2Int> tiles = new HashSet<Vector2Int>(h);
                foreach (var t in v) tiles.Add(t);

                AddGroup(new List<Vector2Int>(tiles), shape, consumed, results);
            }
        }
    }

    private static void DetectSquare2x2(Tile[,] grid, int cols, int rows, HashSet<Vector2Int> consumed, List<MatchGroup> results)
    {
        for (int x = 0; x < cols - 1; x++)
        {
            for (int y = 0; y < rows - 1; y++)
            {
                var tiles = new List<Vector2Int>
                {
                    new Vector2Int(x, y),
                    new Vector2Int(x + 1, y),
                    new Vector2Int(x, y + 1),
                    new Vector2Int(x + 1, y + 1)
                };

                if (tiles.Exists(c => consumed.Contains(c))) continue;
                if (!AllSameType(grid, tiles)) continue;

                AddGroup(tiles, MatchShape.Square2x2, consumed, results);
            }
        }
    }

    private static void DetectLongRuns(List<List<Vector2Int>> runs, HashSet<Vector2Int> consumed, List<MatchGroup> results, int minimumLength = MinRunLength)
    {
        foreach (var run in runs)
        {
            if (run.Count < minimumLength) continue;

            List<Vector2Int> remaining = run.FindAll(t => !consumed.Contains(t));
            if (remaining.Count < minimumLength) continue;

            MatchShape shape = remaining.Count >= 5 ? MatchShape.Five
                             : remaining.Count == 4 ? MatchShape.Four
                             : MatchShape.Three;

            AddGroup(remaining, shape, consumed, results);
        }
    }

    private static void AddGroup(List<Vector2Int> tiles, MatchShape shape, HashSet<Vector2Int> consumed, List<MatchGroup> results)
    {
        foreach (var t in tiles) consumed.Add(t);
        results.Add(new MatchGroup(tiles, shape));
    }

    private static List<Vector2Int> FindOverlap(List<Vector2Int> a, List<Vector2Int> b)
    {
        HashSet<Vector2Int> setB = new HashSet<Vector2Int>(b);
        List<Vector2Int> overlap = new List<Vector2Int>();
        foreach (var t in a)
            if (setB.Contains(t)) overlap.Add(t);
        return overlap;
    }

    private static bool AllSameType(Tile[,] grid, List<Vector2Int> tiles)
    {
        PetalType? type = null;
        foreach (var c in tiles)
        {
            Tile tile = grid[c.x, c.y];
            if (tile == null || !tile.IsMatchable()) return false;
            if (type == null) type = tile.Petal.PetalType;
            else if (tile.Petal.PetalType != type) return false;
        }
        return true;
    }
}
