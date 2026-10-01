using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Raises this UI above a tutorial dimmer and restores its presentation afterward.
    /// </summary>
    public class UITutorialTarget : MonoBehaviour
    {
        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GraphicRaycaster targetGraphicRaycaster;

        private bool isTutorialHighlightActive;
        private bool previousOverrideSorting;
        private int previousSortingLayerId;
        private int previousSortingOrder;
        private bool previousRaycasterEnabled;

        #region Unity Lifecycle

        private void OnDisable()
        {
            RestoreTutorialTarget();
        }

        #endregion

        #region Public API

        public RectTransform TargetRectTransform => targetRectTransform;

        public void RaiseTutorialTarget(int sortingLayerId, int sortingOrder, bool allowTargetInput)
        {
            if (!isTutorialHighlightActive)
            {
                previousOverrideSorting = targetCanvas.overrideSorting;
                previousSortingLayerId = targetCanvas.sortingLayerID;
                previousSortingOrder = targetCanvas.sortingOrder;
                previousRaycasterEnabled = targetGraphicRaycaster.enabled;
                isTutorialHighlightActive = true;
            }

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingLayerID = sortingLayerId;
            targetCanvas.sortingOrder = sortingOrder;
            targetGraphicRaycaster.enabled = allowTargetInput;
        }

        public void RestoreTutorialTarget()
        {
            if (!isTutorialHighlightActive) return;
            targetCanvas.sortingLayerID = previousSortingLayerId;
            targetCanvas.sortingOrder = previousSortingOrder;
            targetCanvas.overrideSorting = previousOverrideSorting;
            targetGraphicRaycaster.enabled = previousRaycasterEnabled;
            isTutorialHighlightActive = false;
        }

        #endregion
    }
}
