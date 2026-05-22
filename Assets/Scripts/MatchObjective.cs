using System.Collections.Generic;
using System.Linq;

namespace DefaultNamespace
{
    public class MatchObjective : IObjective
    {
        private List<PetalGoal> goals;
        private Dictionary<PetalType, int> progress;

        public MatchObjective(ObjectiveData data)
        {
            goals = data.petals;
        }

        public bool CheckObjective()
        {
            return goals.All(g => g.amount <= 0);
        }
    }
}