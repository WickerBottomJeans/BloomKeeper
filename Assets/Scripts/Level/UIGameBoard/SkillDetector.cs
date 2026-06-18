using System;
using System.Collections.Generic;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Detects swap-triggered skill activations. Returns activations only — execution is handled by SkillManager.
    /// </summary>
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

        private static readonly Dictionary<SkillKey, Func<Tile[,], Vector2Int, Vector2Int, List<SkillActivation>>> _handlers =
            new()
            {
                {
                    new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.None),
                    HandleSunburst
                },
                {
                    new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.StripedHorizontal),
                    HandleStripeSunburst
                },
                {
                    new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.StripedVertical),
                    HandleStripeSunburst
                },
            };

        public static List<SkillActivation> DetectOnSwap(Tile[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            Petal petalA = grid[cellA.x, cellA.y].Petal;
            Petal petalB = grid[cellB.x, cellB.y].Petal;

            SpecialSkillType skillA = petalA?.Skill ?? SpecialSkillType.None;
            SpecialSkillType skillB = petalB?.Skill ?? SpecialSkillType.None;

            var key = new SkillKey(skillA, skillB);

            if (!_handlers.TryGetValue(key, out var handler))
                return new List<SkillActivation>();

            return handler(grid, cellA, cellB);
        }

        private static List<SkillActivation> HandleSunburst(Tile[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            Vector2Int sunburstCell = grid[cellA.x, cellA.y].Petal?.Skill == SpecialSkillType.Sunburst ? cellA : cellB;
            Vector2Int causerCell = sunburstCell == cellA ? cellB : cellA;

            Petal selfPetal = new Petal(grid[sunburstCell.x, sunburstCell.y].Petal);
            Petal causerPetal = grid[causerCell.x, causerCell.y].Petal != null
                ? new Petal(grid[causerCell.x, causerCell.y].Petal)
                : null;

            return new List<SkillActivation>
            {
                new SkillActivation(sunburstCell, SpecialSkillType.Sunburst, selfPetal, causerPetal)
            };
        }
        
        private static List<SkillActivation> HandleStripeSunburst(Tile[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            Vector2Int stripeCell = grid[cellA.x, cellA.y].Petal?.Skill is SpecialSkillType.StripedHorizontal or SpecialSkillType.StripedVertical ? cellA : cellB;
            Vector2Int sunburstCell = stripeCell == cellA ? cellB : cellA;

            Petal selfPetal = new Petal(grid[stripeCell.x, stripeCell.y].Petal.PetalType, SpecialSkillType.StripeSunburst);
            PetalType targetType = grid[stripeCell.x, stripeCell.y].Petal.PetalType;
            SpecialSkillType stripeDirection = grid[stripeCell.x, stripeCell.y].Petal.Skill;

            return new List<SkillActivation>
            {
                new SkillActivation(sunburstCell, SpecialSkillType.StripeSunburst, selfPetal, null, new ComboData(targetType, stripeDirection))
            };
        }
    }
}