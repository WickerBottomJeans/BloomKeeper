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

        public void Init(List<IObjective> objectives, List<ConstrainerViewData> constrainerViewData)
        {
            levelHud.Init(objectives, constrainerViewData);
            boosterBoard?.Show();
        }

        public void RefreshObjectives()
        {
            levelHud.RefreshObjectives();
        }

        public void DisplayConstrainers(List<ConstrainerViewData> viewData)
        {
            levelHud.DisplayConstrainers(viewData);
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
