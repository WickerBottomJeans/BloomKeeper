using System;

namespace DefaultNamespace
{
    public class LevelSessionFlow
    {
        public event Action<LevelSessionResult> LevelFinished;

        public void PrepareLevel(int levelId)
        {
            EnterPreparing(levelId);
        }

        public void StartPreparedLevel()
        {
            EnterPlaying();
        }

        public void LeaveLevel()
        {
            EnterExiting();
        }

        private void EnterPreparing(int levelId)
        {
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelSessionManager.Instance.OnLevelFinished += HandleLevelFinished;
            LevelSessionManager.Instance.PrepareLevelSession(levelId);
        }

        private void EnterPlaying()
        {
            LevelSessionManager.Instance.StartPreparedLevelSession();
            ApplicationInputController.Instance.SetGameBoardInputActive(true);
        }

        private void EnterResultHold(LevelSessionResult result)
        {
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelFinished?.Invoke(result);
        }

        private void EnterExiting()
        {
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelSessionManager.Instance.OnLevelFinished -= HandleLevelFinished;
            LevelSessionManager.Instance.ClearCurrentLevelSession();
        }

        private void HandleLevelFinished(LevelSessionResult result)
        {
            EnterResultHold(result);
        }
    }
}
