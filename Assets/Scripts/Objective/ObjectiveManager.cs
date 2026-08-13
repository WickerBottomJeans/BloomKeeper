using System;
using System.Collections.Generic;
using System.Linq;

using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class ObjectiveManager : IGameplayEventHandler<BoardResolutionStepCompletedEvent>
    {
        private readonly List<IObjective> objectives;

        public event Action OnAllObjectedCompleted;
        public event Action OnProgressUpdated;

        public ObjectiveManager(List<IObjective> objectives)
        {
            this.objectives = objectives;
        }

        public void Handle(BoardResolutionStepCompletedEvent gameplayEvent)
        {
            foreach (IObjective objective in objectives)
                objective.Apply(gameplayEvent.Result.TileChanges);

            OnProgressUpdated?.Invoke();

            if (objectives.All(o => o.CheckObjective()))
                OnAllObjectedCompleted?.Invoke();
        }

        public bool AllComplete => objectives.All(o => o.CheckObjective());

        public List<ObjectiveViewData> GetViewData()
        {
            List<ObjectiveViewData> viewData = new();
            foreach (IObjective objective in objectives)
                viewData.AddRange(objective.GetViewData());
            return viewData;
        }

        public IReadOnlyList<ObjectiveTileTargetGroup> GetTargetGroups(IReadOnlyList<TileState> boardSnapshot)
        {
            var targetGroups = new List<ObjectiveTileTargetGroup>();

            foreach (IObjective objective in objectives)
            {
                if (objective is not IObjectiveTileTargetProvider targetProvider) continue;
                targetGroups.AddRange(targetProvider.GetTargetGroups(boardSnapshot));
            }

            return targetGroups;
        }
    }
}
