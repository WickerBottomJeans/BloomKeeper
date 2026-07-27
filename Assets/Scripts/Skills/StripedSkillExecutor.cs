using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public sealed class StripedSkillExecutor : ISkillExecutor
    {
        public SkillUseResult Execute(SkillExecutionContext context, SkillActivation activation)
        {
            BoardCell[,] grid = context.Grid;
            Vector2Int skillPosition = activation.ParticipantA.Position;
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var positions = new List<Vector2Int>();

            if (activation.EffectType == SpecialSkillType.StripedHorizontal)
            {
                for (int x = 0; x < columns; x++)
                    positions.Add(new Vector2Int(x, skillPosition.y));
            }
            else if (activation.EffectType == SpecialSkillType.StripedVertical)
            {
                for (int y = 0; y < rows; y++)
                    positions.Add(new Vector2Int(skillPosition.x, y));
            }
            else
            {
                throw new ArgumentException("Not a striped skill type.", nameof(activation.EffectType));
            }

            var matchGroup = new MatchGroup(positions, MatchShape.None, new Petal(activation.ParticipantA.Petal));
            var representation = new StripedRepresentationData(skillPosition, activation.EffectType, positions);
            return new SkillUseResult(matchGroup, representation);
        }
    }
}
