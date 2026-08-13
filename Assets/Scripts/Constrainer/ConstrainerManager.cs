using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ConstrainerManager : IGameplayEventHandler<PlayerMoveCommittedEvent>
    {
        private readonly List<IConstrainer> constrainers;
        private readonly List<IGameplayEventHandler<PlayerMoveCommittedEvent>> playerMoveHandlers = new List<IGameplayEventHandler<PlayerMoveCommittedEvent>>();
        private bool isRunning;

        public ConstrainerManager(List<IConstrainer> constrainers)
        {
            this.constrainers = constrainers;
            foreach (IConstrainer constrainer in constrainers)
            {
                constrainer.OnFailed += HandleConstrainerFailed;
                constrainer.OnProgressUpdated += HandleConstrainerProgressUpdated;
                if (constrainer is IGameplayEventHandler<PlayerMoveCommittedEvent> playerMoveHandler)
                    playerMoveHandlers.Add(playerMoveHandler);
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

        public void Handle(PlayerMoveCommittedEvent gameplayEvent)
        {
            if (!isRunning) return;

            foreach (IGameplayEventHandler<PlayerMoveCommittedEvent> handler in playerMoveHandlers)
                handler.Handle(gameplayEvent);
        }

        private void HandleConstrainerProgressUpdated()
        {
            OnProgressUpdated?.Invoke();
        }

        private void HandleConstrainerFailed(ConstrainerFailureData failureData)
        {
            OnFailed?.Invoke(failureData);
        }
    }
}
