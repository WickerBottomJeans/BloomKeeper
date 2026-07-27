using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UILevelUI : MonoBehaviour
    {
        [SerializeField] private UILevelHud levelHud;
        [SerializeField] private UIBoosterBoard boosterBoard;
        [SerializeField] private RectTransform boardPlayArea;

        public event Action PauseRequested
        {
            add => levelHud.PauseRequested += value;
            remove => levelHud.PauseRequested -= value;
        }

        public void Init(List<ObjectiveViewData> objectiveViewData, List<ConstrainerViewData> constrainerViewData, int scoreTarget, IReadOnlyList<int> scoreMilestones, int starCap)
        {
            levelHud.Init(objectiveViewData, constrainerViewData, scoreTarget, scoreMilestones, starCap);
            boosterBoard.Show();
        }

        public void RefreshObjectives(List<ObjectiveViewData> objectiveViewData)
        {
            levelHud.RefreshObjectives(objectiveViewData);
        }

        public void DisplayConstrainers(List<ConstrainerViewData> viewData)
        {
            levelHud.DisplayConstrainers(viewData);
        }

        public void InitScoreBoard(int targetScore, IReadOnlyList<int> milestoneScores, int starCap)
        {
            levelHud.InitScoreBoard(targetScore, milestoneScores, starCap);
        }

        public void DisplayScore(int score, int stars)
        {
            levelHud.DisplayScore(score, stars);
        }

        public Rect GetBoardPlayAreaScreenRect(Camera uiCamera)
        {
            Canvas.ForceUpdateCanvases();

            Vector3[] worldCorners = new Vector3[4];
            boardPlayArea.GetWorldCorners(worldCorners);

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[2]);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
