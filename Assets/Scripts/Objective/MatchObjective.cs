using System.Collections.Generic;
using System;
using System.Linq;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using Petals;
using UnityEngine;

namespace DefaultNamespace
{
    public class MatchObjective : IObjective, IObjectiveTileTargetProvider
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

        public void Apply(IReadOnlyList<TileChange> changes)
        {
            foreach (TileChange change in changes)
            {
                if (!change.PetalWasRemoved) continue;
                PetalGoal goal = goals.FirstOrDefault(g => g.petalType == change.RemovedPetalType);
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

        public IReadOnlyList<ObjectiveTileTargetGroup> GetTargetGroups(IReadOnlyList<TileState> boardSnapshot)
        {
            var petalPositions = new List<Vector2Int>();
            if (CheckObjective()) return Array.Empty<ObjectiveTileTargetGroup>();

            foreach (TileState tile in boardSnapshot)
            {
                if (!tile.CanClearPetal || !tile.PetalType.HasValue) continue;
                if (goals.Any(goal => goal.amount > 0 && goal.petalType == tile.PetalType.Value))
                    petalPositions.Add(tile.Position);
            }

            if (petalPositions.Count == 0) return Array.Empty<ObjectiveTileTargetGroup>();
            return new[] { new ObjectiveTileTargetGroup(ObjectiveType, petalPositions) };
        }
    }
}
