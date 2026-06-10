using System;
using System.Collections.Generic;
using System.Linq;

namespace DefaultNamespace
{
    public class ObjectiveManager
    {
        private List<IObjective> objectives;
        public event Action OnAllComplete;
        public event Action OnProgressUpdated;

        public ObjectiveManager(List<IObjective> objectives)
        {
            this.objectives = objectives;
        }

        public void Report(ObjectiveDTO e)
        {
            foreach (IObjective objective in objectives)
                objective.Report(e);

            OnProgressUpdated?.Invoke();

            if (objectives.All(o => o.CheckObjective()))
                OnAllComplete?.Invoke();
        }

        public bool AllComplete => objectives.All(o => o.CheckObjective());
    }
}