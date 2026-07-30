using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public sealed class BubbleSkillExecutor : ISkillExecutor
    {
        private const int Range = 1;

        public SkillUseResult Execute(SkillExecutionContext context, SkillActivation activation)
        {
            SkillParticipant consumedInput = activation.GetOnlyConsumedInput();
            Tile[,] grid = context.Grid;
            Vector2Int center = consumedInput.Position;
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var positions = new List<Vector2Int>();

            for (int x = center.x - Range; x <= center.x + Range; x++)
            for (int y = center.y - Range; y <= center.y + Range; y++)
            {
                if (x < 0 || x >= columns || y < 0 || y >= rows) continue;
                positions.Add(new Vector2Int(x, y));
            }

            var matchGroup = new MatchGroup(positions, MatchShape.None, new Petal(consumedInput.Petal));
            var representation = new BubbleRepresentationData(center, positions, activation.GetConsumedInputPositions());
            return new SkillUseResult(matchGroup, representation);
        }
    }
}
