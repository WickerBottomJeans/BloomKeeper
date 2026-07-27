using System.Collections.Generic;
using System;
using System.Linq;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using Petals;
using UnityEngine;

namespace DefaultNamespace
{
    public class MatchObjective : IObjective, IGameplayEventHandler, IObjectiveBoardCellTargetProvider
    {
        private List<PetalGoal> goals;

        public MatchObjective(ObjectiveJson json)
        {
            goals = json.petals
                .Select(goal => new PetalGoal
                {
                    petalType = goal.petalType,
                    amount = goal.amount
                })
                .ToList();
        }

        public ObjectiveType ObjectiveType { get; } = ObjectiveType.Match;
        public bool CheckObjective() => goals.All(g => g.amount <= 0);
        public Type HandledEventType => typeof(PetalsClearedEvent);

        public void Handle(IGameplayEvent e)
        {
            PetalsClearedEvent cleared = (PetalsClearedEvent)e;
            foreach (PetalType petalType in cleared.ClearedPetals)
            {
                PetalGoal goal = goals.FirstOrDefault(g => g.petalType == petalType);
                if (goal != null) goal.amount--;
            }
        }

        public List<ObjectiveViewData> GetViewData()
        {
            return goals.ConvertAll(g => new ObjectiveViewData
            {
                spriteKey = SpriteKeyHelper.GetPetalSpriteKey(g.petalType, SpecialSkillType.None),
                objectiveText = Mathf.Max(0, g.amount).ToString(),
                remainingAmount = Mathf.Max(0, g.amount)
            });
        }

        public IReadOnlyList<ObjectiveBoardCellTargetGroup> GetTargetGroups(BoardCell[,] grid)
        {
            var petalPositions = new List<Vector2Int>();
            if (CheckObjective()) return Array.Empty<ObjectiveBoardCellTargetGroup>();

            for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                BoardCell cell = grid[x, y];
                if (!cell.CanClearPetal()) continue;
                if (goals.Any(goal => goal.amount > 0 && goal.petalType == cell.Petal.PetalType))
                    petalPositions.Add(new Vector2Int(x, y));
            }

            if (petalPositions.Count == 0) return Array.Empty<ObjectiveBoardCellTargetGroup>();
            return new[] { new ObjectiveBoardCellTargetGroup(ObjectiveType, petalPositions) };
        }
    }
}
