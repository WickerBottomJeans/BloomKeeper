using System.Collections.Generic;
using System.Linq;

namespace DefaultNamespace
{
    public class MatchObjective : IObjective
    {
        private List<PetalGoal> goals;

        public MatchObjective(ObjectiveJson json)
        {
            goals = json.petals;
        }

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
    }
}