using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class LevelSessionFlow
    {
        public event Action<LevelSessionResult> LevelFinished;
        public event Action QuitLevelRequested;
        public event Action SettingsRequested;

        private bool isPaused;

        public async UniTask PrepareLevel(int levelId)
        {
            LevelData levelData = await ConfigManager.Instance.GetLevelDataAsync(levelId);
            EnterPreparing(levelData);
        }

        public void StartPreparedLevel()
        {
            EnterPlaying();
        }

        public void LeaveLevel()
        {
            EnterExiting();
        }

        private void EnterPreparing(LevelData levelData)
        {
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            isPaused = false;
            LevelSessionManager.Instance.OnLevelFinished += HandleLevelFinished;
            LevelSessionManager.Instance.PrepareLevelSession(levelData);
        }

        private void EnterPlaying()
        {
            LevelSessionManager.Instance.StartPreparedLevelSession();
            LevelSessionManager.Instance.BoosterUseFailed += HandleBoosterUseFailed;
            UIManager.Instance.LevelPauseRequested += HandlePauseRequested;
            UIManager.Instance.BoosterUseRequested += HandleBoosterUseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(true);
        }

        private void EnterResultHold(LevelSessionResult result)
        {
            LevelSessionManager.Instance.BoosterUseFailed -= HandleBoosterUseFailed;
            UIManager.Instance.LevelPauseRequested -= HandlePauseRequested;
            UIManager.Instance.BoosterUseRequested -= HandleBoosterUseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelFinished?.Invoke(result);
        }

        private void EnterExiting()
        {
            if (isPaused) HidePauseMenu();
            LevelSessionManager.Instance.BoosterUseFailed -= HandleBoosterUseFailed;
            UIManager.Instance.LevelPauseRequested -= HandlePauseRequested;
            UIManager.Instance.BoosterUseRequested -= HandleBoosterUseRequested;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelSessionManager.Instance.OnLevelFinished -= HandleLevelFinished;
            LevelSessionManager.Instance.ClearCurrentLevelSession();
            isPaused = false;
        }

        private void HandleLevelFinished(LevelSessionResult result)
        {
            EnterResultHold(result);
        }

        private void HandlePauseRequested()
        {
            PauseSession();
            ShowPauseMenu();
        }

        private void HandleBoosterUseRequested(BoosterType boosterType)
        {
            LevelSessionManager.Instance.RequestBoosterUse(boosterType);
        }

        private void HandleBoosterUseFailed()
        {
            PauseSession();
            ApplicationOperationRunner.Instance.Run(RunBoosterUseFailureDialogAsync);
        }

        private async UniTask RunBoosterUseFailureDialogAsync()
        {
            DialogOptionButton[] options = { DialogOptionButton.Ok };
            await DialogManager.Instance.RunDialogWorkflow("Oops!", "Something went wrong while using that booster. Sorry about that—you can continue playing the level.", async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                if ((DialogButtonType)buttonId != DialogButtonType.Ok)
                    throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported booster-use failure dialog button.");
            }, options);

            ResumeSession();
        }

        private void HandleResumeRequested()
        {
            HidePauseMenu();
            ResumeSession();
        }

        private void HandleQuitRequested()
        {
            if (!isPaused)
                throw new InvalidOperationException("Cannot quit through the pause menu while the level session is not paused.");

            QuitLevelRequested?.Invoke();
        }

        private void HandleSettingsRequested()
        {
            if (!isPaused)
                throw new InvalidOperationException("Cannot open Settings through the pause menu while the level session is not paused.");

            SettingsRequested?.Invoke();
        }

        private void PauseSession()
        {
            if (isPaused) throw new InvalidOperationException("Cannot pause a level session that is already paused.");

            isPaused = true;
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            LevelSessionManager.Instance.PauseCurrentLevelSession();
        }

        private void ResumeSession()
        {
            if (!isPaused) throw new InvalidOperationException("Cannot resume a level session that is not paused.");

            LevelSessionManager.Instance.ResumeCurrentLevelSession();
            isPaused = false;
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
