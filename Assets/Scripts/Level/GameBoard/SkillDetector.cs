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

        private static readonly Dictionary<SkillKey, Func<SkillParticipant, SkillParticipant, List<SkillActivation>>> _handlers =
            new()
            {
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.None), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.StripedHorizontal), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.StripedVertical), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.Bubble), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.PrismaticBloom, SpecialSkillType.Butterfly), HandlePrismaticBloom },
                { new SkillKey(SpecialSkillType.StripedHorizontal, SpecialSkillType.StripedHorizontal), HandleStripeStripeFusion },
                { new SkillKey(SpecialSkillType.StripedHorizontal, SpecialSkillType.StripedVertical), HandleStripeStripeFusion },
                { new SkillKey(SpecialSkillType.StripedVertical, SpecialSkillType.StripedVertical), HandleStripeStripeFusion },
            };

        public static List<SkillActivation> DetectOnSwap(Tile[,] grid, Vector2Int tileA, Vector2Int tileB)
        {
            if (grid[tileA.x, tileA.y].Piece is not Petal petalA || grid[tileB.x, tileB.y].Piece is not Petal petalB) return new List<SkillActivation>();

            SpecialSkillType skillA = petalA.Skill;
            SpecialSkillType skillB = petalB.Skill;

            var key = new SkillKey(skillA, skillB);

            if (!_handlers.TryGetValue(key, out var handler))
                return new List<SkillActivation>();

            var swapInitiator = new SkillParticipant(tileB, new Petal(petalB));
            var swapPartner = new SkillParticipant(tileA, new Petal(petalA));
            return handler(swapInitiator, swapPartner);
        }

        public static bool HasActivationOnSwap(SpecialSkillType skillA, SpecialSkillType skillB)
        {
            return _handlers.ContainsKey(new SkillKey(skillA, skillB));
        }

        private static List<SkillActivation> HandlePrismaticBloom(SkillParticipant swapInitiator, SkillParticipant swapPartner)
        {
            return new List<SkillActivation>
            {
                SkillActivation.FromSwap(SkillExecutionType.PrismaticBloom, swapInitiator, swapPartner)
            };
        }

        private static List<SkillActivation> HandleStripeStripeFusion(SkillParticipant swapInitiator, SkillParticipant swapPartner)
        {
            return new List<SkillActivation>
            {
                SkillActivation.FromSwap(SkillExecutionType.StripeStripeFusion, swapInitiator, swapPartner)
            };
        }
    }
}
