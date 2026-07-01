using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILevelUI levelUIPrefab;
        private UILevelUI levelUIInstance;

        public void ShowLevelUI(List<IObjective> objectives, List<ConstrainerViewData> constrainerViewData, int scoreTarget, IReadOnlyList<int> scoreMilestones, int starCap)
        {
            if (levelUIPrefab == null) return;
            if (levelUIInstance == null)
                levelUIInstance = Instantiate(levelUIPrefab, canvas.transform);
            levelUIInstance.Init(objectives, constrainerViewData, scoreTarget, scoreMilestones, starCap);
            levelUIInstance.Show();
        }

        public void HideLevelUI()
        {
            levelUIInstance?.Hide();
        }

        public void RefreshLevelObjectives()
        {
            levelUIInstance?.RefreshObjectives();
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
    }
}
