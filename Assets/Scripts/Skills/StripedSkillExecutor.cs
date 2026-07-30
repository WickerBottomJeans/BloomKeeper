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
            SkillParticipant consumedInput = activation.GetOnlyConsumedInput();
            Tile[,] grid = context.Grid;
            Vector2Int skillPosition = consumedInput.Position;
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var positions = new List<Vector2Int>();
            SpecialSkillType direction = consumedInput.Petal.Skill;

            if (direction == SpecialSkillType.StripedHorizontal)
            {
                for (int x = 0; x < columns; x++)
                    positions.Add(new Vector2Int(x, skillPosition.y));
            }
            else if (direction == SpecialSkillType.StripedVertical)
            {
                for (int y = 0; y < rows; y++)
                    positions.Add(new Vector2Int(skillPosition.x, y));
            }
            else
            {
                throw new ArgumentException("Not a striped skill type.", nameof(activation.EffectType));
            }

            var matchGroup = new MatchGroup(positions, MatchShape.None, new Petal(consumedInput.Petal));
            var representation = new StripedRepresentationData(skillPosition, direction, positions, activation.GetConsumedInputPositions());
            return new SkillUseResult(matchGroup, representation);
        }
    }
}
