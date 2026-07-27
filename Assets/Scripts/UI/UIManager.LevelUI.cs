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

        public void ShowLevelUI(List<ObjectiveViewData> objectiveViewData, List<ConstrainerViewData> constrainerViewData, int scoreTarget, IReadOnlyList<int> scoreMilestones, int starCap)
        {
            if (levelUIPrefab == null) return;
            if (levelUIInstance == null)
                levelUIInstance = Instantiate(levelUIPrefab, uiRoot);
            levelUIInstance.PauseRequested -= HandleLevelPauseRequested;
            levelUIInstance.PauseRequested += HandleLevelPauseRequested;
            levelUIInstance.Init(objectiveViewData, constrainerViewData, scoreTarget, scoreMilestones, starCap);
            levelUIInstance.Show();
        }

        public void HideLevelUI()
        {
            if (levelUIInstance != null)
                levelUIInstance.PauseRequested -= HandleLevelPauseRequested;
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
    }
}
