using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public static class SkillDetector
    {
        private struct SkillKey : IEquatable<SkillKey>
        {
            public readonly SpecialSkillType A;
            public readonly SpecialSkillType B;

            public SkillKey(SpecialSkillType a, SpecialSkillType b)
            {
                A = a < b ? a : b;
                B = a < b ? b : a;
            }

            public bool Equals(SkillKey other) => A == other.A && B == other.B;
            public override int GetHashCode() => HashCode.Combine(A, B);
        }

        private static readonly Dictionary<SkillKey, Func<Tile[,], Vector2Int, Vector2Int, List<MatchGroup>>> _handlers =
            new()
            {
                {
                    new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.None),
                    HandleSunburst
                },
            };

        public static List<MatchGroup> DetectOnSwap(Tile[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            Petal petalA = grid[cellA.x, cellA.y].Petal;
            Petal petalB = grid[cellB.x, cellB.y].Petal;

            SpecialSkillType skillA = petalA?.Skill ?? SpecialSkillType.None;
            SpecialSkillType skillB = petalB?.Skill ?? SpecialSkillType.None;

            var key = new SkillKey(skillA, skillB);

            if (!_handlers.TryGetValue(key, out var handler))
                return new List<MatchGroup>();

            return handler(grid, cellA, cellB);
        }

        private static List<MatchGroup> HandleSunburst(Tile[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            // cellA is the sunburst cell, cellB is the causer
            Vector2Int sunburstCell = grid[cellA.x, cellA.y].Petal?.Skill == SpecialSkillType.Sunburst ? cellA : cellB;
            Vector2Int causerCell = sunburstCell == cellA ? cellB : cellA;

            Petal causerPetal = grid[causerCell.x, causerCell.y].Petal != null
                ? new Petal(grid[causerCell.x, causerCell.y].Petal)
                : null;

            return new List<MatchGroup>
            {
                new MatchGroup(new List<Vector2Int> { sunburstCell }, MatchShape.None, causerPetal)
            };
        }
    }
}