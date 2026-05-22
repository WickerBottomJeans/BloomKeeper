using System.Collections.Generic;
using System.Linq;

namespace DefaultNamespace
{
    public class LevelManager
    {
        private List<IObjective> objectives;

        public void Init(int LevelID)
        {
            LevelData data = LevelLoader.Load(LevelID);
            this.objectives = data.objectives
                .Select(o => ObjectiveFactory.Create(o))
                .ToList();
        }
    }
}