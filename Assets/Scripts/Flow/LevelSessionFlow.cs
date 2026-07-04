using System;

namespace DefaultNamespace
{
    public class LevelSessionFlow
    {
        public event Action<LevelSessionResult> LevelFinished;

        public void StartLevel(int levelId)
        {
            EnterStarting(levelId);
        }

        public void LeaveLevel()
        {
            EnterExiting();
        }

        private void EnterStarting(int levelId)
        {
            LevelSessionManager.Instance.SetPlayerActionsEnabled(false);
            LevelSessionManager.Instance.OnLevelFinished += HandleLevelFinished;
            LevelSessionManager.Instance.StartLevelSession(levelId);
            EnterPlaying();
        }

        private void EnterPlaying()
        {
            LevelSessionManager.Instance.SetPlayerActionsEnabled(true);
        }

        private void EnterResultHold(LevelSessionResult result)
        {
            LevelSessionManager.Instance.SetPlayerActionsEnabled(false);
            LevelFinished?.Invoke(result);
        }

        private void EnterExiting()
        {
            LevelSessionManager.Instance.OnLevelFinished -= HandleLevelFinished;
            LevelSessionManager.Instance.ClearCurrentLevelSession();
        }

        private void HandleLevelFinished(LevelSessionResult result)
        {
            EnterResultHold(result);
        }
    }
}
