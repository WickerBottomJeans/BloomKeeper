using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly Dictionary<Type, List<IObjectiveEventHandler>> handlersByEventType = new();

        public event Action OnAllComplete;
        public event Action OnProgressUpdated;

        public ObjectiveManager(List<IObjective> objectives)
        {
            this.objectives = objectives;
            foreach (IObjective objective in objectives)
                RegisterHandlers(objective);
        }

        public void Report(IObjectiveEvent e)
        {
            Type eventType = e.GetType();
            if (!handlersByEventType.TryGetValue(eventType, out List<IObjectiveEventHandler> handlers))
                return;

            foreach (IObjectiveEventHandler handler in handlers)
                handler.Handle(e);

            OnProgressUpdated?.Invoke();

            if (objectives.All(o => o.CheckObjective()))
                OnAllComplete?.Invoke();
        }

        public bool AllComplete => objectives.All(o => o.CheckObjective());

        private void RegisterHandlers(IObjective objective)
        {
            if (objective is not IObjectiveEventHandler handler) return;

            Type eventType = handler.HandledEventType;
            if (!handlersByEventType.TryGetValue(eventType, out List<IObjectiveEventHandler> handlers))
            {
                handlers = new List<IObjectiveEventHandler>();
                handlersByEventType[eventType] = handlers;
            }

            handlers.Add(handler);
        }
    }
}
