using System;
using System.Collections.Generic;
using System.Linq;

using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class ObjectiveManager
    {
        private readonly List<IObjective> objectives;


        /// <summary>
        /// Key: objective event type, like PetalsClearedEvent.
        /// Value: objective event handlers that declared they handle that event type.
        /// Example: PetalsClearedEvent -> [MatchObjective instance, ...]
        /// </summary>
        private readonly Dictionary<Type, List<IGameplayEventHandler>> handlersByEventType = new();

        public event Action OnAllComplete;
        public event Action OnProgressUpdated;

        public ObjectiveManager(List<IObjective> objectives)
        {
            this.objectives = objectives;
            foreach (IObjective objective in objectives)
                RegisterHandlers(objective);
        }

        public void Report(IGameplayEvent e)
        {
            Type eventType = e.GetType();
            if (!handlersByEventType.TryGetValue(eventType, out List<IGameplayEventHandler> handlers))
                return;

            foreach (IGameplayEventHandler handler in handlers)
                handler.Handle(e);

            OnProgressUpdated?.Invoke();

            if (objectives.All(o => o.CheckObjective()))
                OnAllComplete?.Invoke();
        }

        public bool AllComplete => objectives.All(o => o.CheckObjective());

        public List<ObjectiveViewData> GetViewData()
        {
            List<ObjectiveViewData> viewData = new();
            foreach (IObjective objective in objectives)
                viewData.AddRange(objective.GetViewData());
            return viewData;
        }

        public IReadOnlyList<ObjectiveBoardCellTargetGroup> GetTargetGroups(BoardCell[,] grid)
        {
            var targetGroups = new List<ObjectiveBoardCellTargetGroup>();

            foreach (IObjective objective in objectives)
            {
                if (objective is not IObjectiveBoardCellTargetProvider targetProvider) continue;
                targetGroups.AddRange(targetProvider.GetTargetGroups(grid));
            }

            return targetGroups;
        }

        private void RegisterHandlers(IObjective objective)
        {
            if (objective is not IGameplayEventHandler handler) return;

            Type eventType = handler.HandledEventType;
            if (!handlersByEventType.TryGetValue(eventType, out List<IGameplayEventHandler> handlers))
            {
                handlers = new List<IGameplayEventHandler>();
                handlersByEventType[eventType] = handlers;
            }

            handlers.Add(handler);
        }
    }
}
