using System;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class LevelSessionFlow
    {
        public event Action<LevelSessionResult> LevelFinished;
        public event Action QuitLevelRequested;

        private bool isPaused;

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
            isPaused = false;
            LevelSessionManager.Instance.OnLevelFinished += HandleLevelFinished;
            LevelSessionManager.Instance.PrepareLevelSession(levelId);
        }

        private void EnterPlaying()
        {
            LevelSessionManager.Instance.StartPreparedLevelSession();
            UIManager.Instance.LevelPauseRequested += HandlePauseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(true);
        }

        private void EnterResultHold(LevelSessionResult result)
        {
            UIManager.Instance.LevelPauseRequested -= HandlePauseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelFinished?.Invoke(result);
        }

        private void EnterExiting()
        {
            if (isPaused) ExitPaused();
            UIManager.Instance.LevelPauseRequested -= HandlePauseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelSessionManager.Instance.OnLevelFinished -= HandleLevelFinished;
            LevelSessionManager.Instance.ClearCurrentLevelSession();
        }

        private void HandleLevelFinished(LevelSessionResult result)
        {
            EnterResultHold(result);
        }

        private void HandlePauseRequested()
        {
            if (isPaused)
                throw new InvalidOperationException("Cannot pause a level session that is already paused.");

            isPaused = true;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelSessionManager.Instance.PauseCurrentLevelSession();
            UIManager.Instance.PauseMenuResumeRequested += HandleResumeRequested;
            UIManager.Instance.PauseMenuQuitRequested += HandleQuitRequested;
            UIManager.Instance.ShowPauseMenu();
        }

        private void HandleResumeRequested()
        {
            if (!isPaused)
                throw new InvalidOperationException("Cannot resume a level session that is not paused.");

            ExitPaused();
            LevelSessionManager.Instance.ResumeCurrentLevelSession();
            ApplicationInputController.Instance.SetGameBoardInputActive(true);
        }

        private void HandleQuitRequested()
        {
            if (!isPaused)
                throw new InvalidOperationException("Cannot quit through the pause menu while the level session is not paused.");

            QuitLevelRequested?.Invoke();
        }

        private void ExitPaused()
        {
            UIManager.Instance.HidePauseMenu();
            UIManager.Instance.PauseMenuResumeRequested -= HandleResumeRequested;
            UIManager.Instance.PauseMenuQuitRequested -= HandleQuitRequested;
            isPaused = false;
        }
    }
}
