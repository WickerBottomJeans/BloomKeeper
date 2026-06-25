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

        private static readonly Dictionary<SkillKey, Func<BoardCell[,], Vector2Int, Vector2Int, List<SkillActivation>>> _handlers =
            new()
            {
                { new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.None), HandleSunburst },
                { new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.StripedHorizontal), HandleSunburst },
                { new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.StripedVertical), HandleSunburst },
                { new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.Bomb), HandleSunburst },
                { new SkillKey(SpecialSkillType.Sunburst, SpecialSkillType.Butterfly), HandleSunburst },
            };

        public static List<SkillActivation> DetectOnSwap(BoardCell[,] grid, Vector2Int cellA, Vector2Int cellB)
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

        public static bool HasActivationOnSwap(SpecialSkillType skillA, SpecialSkillType skillB)
        {
            return _handlers.ContainsKey(new SkillKey(skillA, skillB));
        }

        private static List<SkillActivation> HandleSunburst(BoardCell[,] grid, Vector2Int cellA, Vector2Int cellB)
        {
            Vector2Int sunburstCell = grid[cellA.x, cellA.y].Petal?.Skill == SpecialSkillType.Sunburst ? cellA : cellB;
            Vector2Int targetCell = sunburstCell == cellA ? cellB : cellA;

            Petal selfPetal = new Petal(grid[sunburstCell.x, sunburstCell.y].Petal);
            Petal targetPetal = grid[targetCell.x, targetCell.y].Petal != null
                ? new Petal(grid[targetCell.x, targetCell.y].Petal)
                : null;
            SpecialSkillType targetSkill = targetPetal?.Skill ?? SpecialSkillType.None;
            SpecialSkillType activationSkill = targetSkill switch
            {
                SpecialSkillType.None => SpecialSkillType.Sunburst,
                SpecialSkillType.StripedHorizontal => SpecialSkillType.StripeSunburst,
                SpecialSkillType.StripedVertical => SpecialSkillType.StripeSunburst,
                SpecialSkillType.Bomb => SpecialSkillType.BouquetSunburst,
                SpecialSkillType.Butterfly => SpecialSkillType.ButterflySunburst,
                _ => throw new ArgumentOutOfRangeException(nameof(targetSkill), targetSkill, "Sunburst combo is not supported.")
            };
            ComboData combo = new ComboData(targetPetal.PetalType, targetSkill, cellA, cellB);
            Vector2 effectOrigin = ((Vector2)cellA + (Vector2)cellB) * 0.5f;

            if (activationSkill != SpecialSkillType.Sunburst)
                selfPetal = new Petal(targetPetal.PetalType, activationSkill);

            return new List<SkillActivation>
            {
                new SkillActivation(sunburstCell, activationSkill, selfPetal, targetPetal, combo, effectOrigin)
            };
        }
    }
}
