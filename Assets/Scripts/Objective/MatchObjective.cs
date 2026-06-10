using System.Collections.Generic;
using System.Linq;
using Petals;
using UnityEngine;

namespace DefaultNamespace
{
    public class MatchObjective : IObjective
    {
        private List<PetalGoal> goals;

        public MatchObjective(ObjectiveJson json)
        {
            goals = json.petals;
        }

        public ObjectiveType ObjectiveType { get; } = ObjectiveType.Match;
        public bool CheckObjective() => goals.All(g => g.amount <= 0);

        public void Report(ObjectiveDTO e)
        {
            if (e is not PetalsClearedEvent cleared) return;
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
                spriteKey = PetalSpriteKey.GetPetalSpriteKey(g.petalType, SpecialSkillType.None),
                objectiveText = Mathf.Max(0, g.amount).ToString()
            });
        }
    }
}