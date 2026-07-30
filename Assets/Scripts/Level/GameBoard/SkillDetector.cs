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
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.None), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.StripedHorizontal), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.StripedVertical), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.Bubble), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.Butterfly), HandlePrismaticBloom },
            };

        public static List<SkillActivation> DetectOnSwap(Tile[,] grid, Vector2Int tileA, Vector2Int tileB)
        {
            Petal petalA = grid[tileA.x, tileA.y].Petal;
            Petal petalB = grid[tileB.x, tileB.y].Petal;

            SpecialSkillType skillA = petalA?.Skill ?? SpecialSkillType.None;
            SpecialSkillType skillB = petalB?.Skill ?? SpecialSkillType.None;

            var key = new SkillKey(skillA, skillB);

            if (!_handlers.TryGetValue(key, out var handler))
                return new List<SkillActivation>();

            return handler(grid, tileA, tileB);
        }

        public static bool HasActivationOnSwap(SpecialSkillType skillA, SpecialSkillType skillB)
        {
            return _handlers.ContainsKey(new SkillKey(skillA, skillB));
        }

        private static List<SkillActivation> HandlePrismaticBloom(Tile[,] grid, Vector2Int tileA, Vector2Int tileB)
        {
            Petal petalA = new Petal(grid[tileA.x, tileA.y].Petal);
            Petal petalB = new Petal(grid[tileB.x, tileB.y].Petal);
            var participantA = new SkillParticipant(tileA, petalA);
            var participantB = new SkillParticipant(tileB, petalB);

            return new List<SkillActivation>
            {
                new SkillActivation(SpecialSkillType.PrismaticBloom, new[] { participantA, participantB })
            };
        }
    }
}
