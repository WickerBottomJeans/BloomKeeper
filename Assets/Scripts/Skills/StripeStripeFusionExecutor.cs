using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public class StripeStripeFusionExecutor : ISkillExecutor
    {
        public SkillUseResult Execute(SkillExecutionContext context, SkillActivation activation)
        {
            if (activation.EffectType != SkillExecutionType.StripeStripeFusion)
                throw new ArgumentException("Activation is not a Stripe + Stripe fusion.", nameof(activation));
            if (!activation.SwapInitiator.HasValue || !activation.SwapPartner.HasValue)
                throw new InvalidOperationException("Stripe + Stripe fusion requires swap initiator and swap partner roles.");
            if (activation.ConsumedInputs.Count != 2)
                throw new InvalidOperationException($"Stripe + Stripe fusion requires exactly two consumed inputs but received {activation.ConsumedInputs.Count}.");

            SkillParticipant swapInitiator = activation.SwapInitiator.Value;
            SkillParticipant swapPartner = activation.SwapPartner.Value;
            if (!IsStriped(swapInitiator.Petal.Skill) || !IsStriped(swapPartner.Petal.Skill))
                throw new InvalidOperationException("Stripe + Stripe fusion requires two striped petals.");

            Vector2Int anchor = swapInitiator.Position;
            Tile[,] grid = context.Grid;
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var affectedPositions = new List<Vector2Int>(columns + rows - 1);

            for (int x = 0; x < columns; x++)
                affectedPositions.Add(new Vector2Int(x, anchor.y));
            for (int y = 0; y < rows; y++)
            {
                if (y != anchor.y)
                    affectedPositions.Add(new Vector2Int(anchor.x, y));
            }

            IReadOnlyList<Vector2Int> consumedInputPositions = activation.GetConsumedInputPositions();
            var inputMatchGroup = new MatchGroup(new List<Vector2Int>(consumedInputPositions), MatchShape.None, isFromSkillCombo: true);
            var effectMatchGroup = new MatchGroup(affectedPositions, MatchShape.None, new Petal(swapInitiator.Petal));
            var representation = new StripeStripeFusionRepresentationData(anchor, affectedPositions, consumedInputPositions);
            return new SkillUseResult(effectMatchGroup, representation, inputMatchGroup);
        }

        private  bool IsStriped(SpecialSkillType skillType)
        {
            return skillType == SpecialSkillType.StripedHorizontal || skillType == SpecialSkillType.StripedVertical;
        }
    }
}
