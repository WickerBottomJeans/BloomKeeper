using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILevelUI levelUIPrefab;
        private UILevelUI levelUIInstance;

        public event Action LevelPauseRequested;
        public event Action<BoosterType> BoosterUseRequested;
        public event Action BoosterCancelRequested;

        public void ShowLevelUI(LevelUIInitData levelUIInitData)
        {
            if (levelUIPrefab == null) return;
            if (levelUIInstance == null)
                levelUIInstance = Instantiate(levelUIPrefab, uiRoot);
            levelUIInstance.PauseRequested -= HandleLevelPauseRequested;
            levelUIInstance.PauseRequested += HandleLevelPauseRequested;
            levelUIInstance.BoosterUseRequested -= HandleBoosterUseRequested;
            levelUIInstance.BoosterUseRequested += HandleBoosterUseRequested;
            levelUIInstance.BoosterCancelRequested -= HandleBoosterCancelRequested;
            levelUIInstance.BoosterCancelRequested += HandleBoosterCancelRequested;
            levelUIInstance.Init(levelUIInitData);
            levelUIInstance.Show();
        }

        public void HideLevelUI()
        {
            if (levelUIInstance != null)
            {
                levelUIInstance.PauseRequested -= HandleLevelPauseRequested;
                levelUIInstance.BoosterUseRequested -= HandleBoosterUseRequested;
                levelUIInstance.BoosterCancelRequested -= HandleBoosterCancelRequested;
            }
            levelUIInstance?.Hide();
        }

        public void RefreshLevelObjectives(List<ObjectiveViewData> objectiveViewData)
        {
            levelUIInstance?.RefreshObjectives(objectiveViewData);
        }

        public void RefreshLevelConstrainers(List<ConstrainerViewData> viewData)
        {
            levelUIInstance?.DisplayConstrainers(viewData);
        }

        public void DisplayLevelScore(int score, int stars)
        {
            levelUIInstance?.DisplayScore(score, stars);
        }

        public void EnterBoosterTargeting(BoosterType boosterType)
        {
            if (levelUIInstance == null) throw new InvalidOperationException("Cannot show booster targeting before the level UI exists.");
            levelUIInstance.EnterBoosterTargeting(boosterType);
        }

        public void ExitBoosterTargeting()
        {
            if (levelUIInstance == null) throw new InvalidOperationException("Cannot hide booster targeting before the level UI exists.");
            levelUIInstance.ExitBoosterTargeting();
        }

        public Rect GetLevelBoardPlayAreaScreenRect()
        {
            if (levelUIInstance == null) return new Rect(0f, 0f, Screen.width, Screen.height);
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return levelUIInstance.GetBoardPlayAreaScreenRect(uiCamera);
        }

        private void HandleLevelPauseRequested()
        {
            LevelPauseRequested?.Invoke();
        }

        private void HandleBoosterUseRequested(BoosterType boosterType)
        {
            BoosterUseRequested?.Invoke(boosterType);
        }

        private void HandleBoosterCancelRequested()
        {
            BoosterCancelRequested?.Invoke();
        }
    }
}
