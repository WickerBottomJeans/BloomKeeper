using System.Collections.Generic;
using System;
using System.Linq;
using DefaultNamespace.Utility;
using Petals;
using UnityEngine;

namespace DefaultNamespace
{
    public class MatchObjective : IObjective, IGameplayEventHandler
    {
        private List<PetalGoal> goals;

        public MatchObjective(ObjectiveJson json)
        {
            goals = json.petals;
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
    }
}
