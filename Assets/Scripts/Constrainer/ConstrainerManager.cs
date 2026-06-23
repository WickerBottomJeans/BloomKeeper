using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ConstrainerManager
    {
        private readonly List<IConstrainer> constrainers;
        private readonly Dictionary<Type, List<IGameplayEventHandler>> handlersByEventType = new();
        private bool isRunning;

        public ConstrainerManager(List<IConstrainer> constrainers)
        {
            this.constrainers = constrainers;
            foreach (IConstrainer constrainer in constrainers)
            {
                constrainer.OnFailed += HandleConstrainerFailed;
                constrainer.OnProgressUpdated += HandleConstrainerProgressUpdated;
                RegisterHandlers(constrainer);
            }
        }

        public event Action<ConstrainerFailureData> OnFailed;
        public event Action OnProgressUpdated;
        public IReadOnlyList<IConstrainer> Constrainers => constrainers;

        public List<ConstrainerViewData> GetViewData()
        {
            List<ConstrainerViewData> viewData = new();
            foreach (IConstrainer constrainer in constrainers)
                viewData.Add(constrainer.GetViewData());
            return viewData;
        }

        public void StartLevel()
        {
            isRunning = true;
        }

        public void StopLevel()
        {
            isRunning = false;
        }

        public void Tick(float deltaTime)
        {
            if (!isRunning) return;

            foreach (IConstrainer constrainer in constrainers)
            {
                if (constrainer is ITickableConstrainer tickableConstrainer)
                    tickableConstrainer.Tick(deltaTime);
            }
        }

        public void Apply(IGameplayEvent e)
        {
            if (!isRunning) return;

            Type eventType = e.GetType();
            if (!handlersByEventType.TryGetValue(eventType, out List<IGameplayEventHandler> handlers))
                return;

            foreach (IGameplayEventHandler handler in handlers)
                handler.Handle(e);
        }

        private void HandleConstrainerProgressUpdated()
        {
            OnProgressUpdated?.Invoke();
        }

        private void HandleConstrainerFailed(ConstrainerFailureData failureData)
        {
            OnFailed?.Invoke(failureData);
        }

        private void RegisterHandlers(IConstrainer constrainer)
        {
            if (constrainer is not IGameplayEventHandler handler) return;

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
