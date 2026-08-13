using System;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;

namespace DefaultNamespace
{
    public class PlayLevelFlow
    {
        private enum State
        {
            Inactive,
            Prepared,
            Playing,
            Paused,
            Finished
        }

        private readonly LevelSessionRuntime levelSessionRuntime;
        private string currentLevelAttemptId;
        private State state;

        public event Action<LevelSessionResult, string> LevelFinished;
        public event Action<string> QuitRequested;
        public event Action SettingsRequested;

        public PlayLevelFlow(LevelSessionRuntime levelSessionRuntime)
        {
            this.levelSessionRuntime = levelSessionRuntime ?? throw new ArgumentNullException(nameof(levelSessionRuntime));
        }

        /// <summary>
        /// [Duong] Prepares and displays an approved level session without starting gameplay.
        /// </summary>
        public void Prepare(LevelData levelData, string levelAttemptId)
        {
            if (state != State.Inactive) throw new InvalidOperationException($"Cannot enter a level while the play flow is {state}.");
            if (!Guid.TryParseExact(levelAttemptId, "N", out _)) throw new ArgumentException("Level attempt ID must be a canonical GUID.", nameof(levelAttemptId));

            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            levelSessionRuntime.OnLevelFinished += HandleLevelFinished;
            currentLevelAttemptId = levelAttemptId;
            levelSessionRuntime.PrepareLevelSession(levelData);
            state = State.Prepared;
        }

        /// <summary>
        /// Starts gameplay for the prepared level session
        /// </summary>
        public void BeginGameplay()
        {
            if (state != State.Prepared) throw new InvalidOperationException($"Cannot begin gameplay while the play flow is {state}.");

            levelSessionRuntime.StartPreparedLevelSession();
            UIManager.Instance.LevelPauseRequested += HandlePauseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(true);
            state = State.Playing;
        }

        /// <summary>
        /// Leaves and clears the level session currently owned by the play flow.
        /// </summary>
        public void Exit()
        {
            if (state == State.Inactive) throw new InvalidOperationException("Cannot leave a level session when none is owned by the play flow.");

            bool wasPaused = state == State.Paused;
            if (wasPaused) HidePauseMenu();
            UIManager.Instance.LevelPauseRequested -= HandlePauseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            levelSessionRuntime.OnLevelFinished -= HandleLevelFinished;
            levelSessionRuntime.ClearCurrentLevelSession();
            if (wasPaused) GameTimeService.ReleasePause(this);
            currentLevelAttemptId = null;
            state = State.Inactive;
        }

        private void HandleLevelFinished(LevelSessionResult result)
        {
            if (state != State.Playing) throw new InvalidOperationException($"Cannot finish gameplay while the play flow is {state}.");

            UIManager.Instance.LevelPauseRequested -= HandlePauseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            state = State.Finished;
            if (currentLevelAttemptId == null) throw new InvalidOperationException("Cannot finish without an active level attempt ID.");
            LevelFinished?.Invoke(result, currentLevelAttemptId);
        }

        private void HandlePauseRequested()
        {
            PauseSession();
            ShowPauseMenu();
        }

        private void HandleResumeRequested()
        {
            HidePauseMenu();
            ResumeSession();
        }

        private void HandleQuitRequested()
        {
            if (state != State.Paused) throw new InvalidOperationException("Cannot quit through the pause menu while the level session is not paused.");
            if (currentLevelAttemptId == null) throw new InvalidOperationException("Cannot quit without an active level attempt ID.");
            QuitRequested?.Invoke(currentLevelAttemptId);
        }

        private void HandleSettingsRequested()
        {
            if (state != State.Paused) throw new InvalidOperationException("Cannot open Settings through the pause menu while the level session is not paused.");
            SettingsRequested?.Invoke();
        }

        private void PauseSession()
        {
            if (state != State.Playing) throw new InvalidOperationException($"Cannot pause gameplay while the play flow is {state}.");

            state = State.Paused;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            levelSessionRuntime.PauseCurrentLevelSession();
            GameTimeService.RequestPause(this);
        }

        private void ResumeSession()
        {
            if (state != State.Paused) throw new InvalidOperationException($"Cannot resume gameplay while the play flow is {state}.");

            GameTimeService.ReleasePause(this);
            levelSessionRuntime.ResumeCurrentLevelSession();
            state = State.Playing;
            ApplicationInputController.Instance.SetGameBoardInputActive(true);
        }

        private void ShowPauseMenu()
        {
            UIManager.Instance.PauseMenuResumeRequested += HandleResumeRequested;
            UIManager.Instance.PauseMenuSettingsRequested += HandleSettingsRequested;
            UIManager.Instance.PauseMenuQuitRequested += HandleQuitRequested;
            UIManager.Instance.ShowPauseMenu();
        }

        private void HidePauseMenu()
        {
            UIManager.Instance.HidePauseMenu();
            UIManager.Instance.PauseMenuResumeRequested -= HandleResumeRequested;
            UIManager.Instance.PauseMenuSettingsRequested -= HandleSettingsRequested;
            UIManager.Instance.PauseMenuQuitRequested -= HandleQuitRequested;
        }
    }
}
