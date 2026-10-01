using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Shows a tutorial board beside its highlighted UI.
    /// </summary>
    public class UITutorial : MonoBehaviour
    {
        [SerializeField] private Canvas dimmerCanvas;
        [SerializeField] private Canvas instructionCanvas;
        [SerializeField] private VerticalLayoutGroup instructionLayoutGroup;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueLabel;
        [SerializeField] private Button skipButton;

        private readonly Vector3[] targetCorners = new Vector3[4];
        private readonly Vector2[] boardPositions = new Vector2[8];
        private RectTransform layoutRoot;
        private RectTransform instructionPanel;
        private float instructionWidth;
        private UITutorialTarget highlightTarget;
        private Camera uiCamera;
        private bool hasWarnedAboutOverlap;

        #region Unity Lifecycle

        private void Awake()
        {
            layoutRoot = (RectTransform)dimmerCanvas.transform;
            instructionPanel = (RectTransform)instructionCanvas.transform;
            instructionWidth = instructionPanel.rect.width;
            continueButton.onClick.AddListener(HandleContinueClicked);
            skipButton.onClick.AddListener(HandleSkipClicked);
        }

        private void LateUpdate()
        {
            if (highlightTarget != null) RefreshTutorialLayout();
        }

        private void OnDisable()
        {
            RestoreHighlightedTarget();
        }

        private void OnDestroy()
        {
            continueButton.onClick.RemoveListener(HandleContinueClicked);
            skipButton.onClick.RemoveListener(HandleSkipClicked);
        }

        #endregion

        #region Public API

        public event Action ContinueRequested;
        public event Action SkipRequested;

        /// <summary>
        /// Shows an instruction; a null button label hides Continue.
        /// </summary>
        public void DisplayTutorial(UITutorialTarget highlightTarget, Camera uiCamera, string message, string continueButtonLabel, bool allowTargetInput)
        {
            RestoreHighlightedTarget();
            this.highlightTarget = highlightTarget;
            this.uiCamera = uiCamera;
            hasWarnedAboutOverlap = false;
            gameObject.SetActive(true);
            instructionPanel.gameObject.SetActive(true);
            instructionCanvas.sortingLayerID = dimmerCanvas.sortingLayerID;
            highlightTarget.RaiseTutorialTarget(dimmerCanvas.sortingLayerID, (dimmerCanvas.sortingOrder + instructionCanvas.sortingOrder) / 2, allowTargetInput);
            instructionText.text = message;
            continueLabel.text = continueButtonLabel;
            continueButton.gameObject.SetActive(continueButtonLabel != null);
            continueButton.interactable = true;
            Canvas.ForceUpdateCanvases();
            RefreshTutorialLayout();
            EventSystem.current.SetSelectedGameObject(continueButtonLabel == null ? null : continueButton.gameObject);
        }

        /// <summary>
        /// Blocks input while the caller waits for a result.
        /// </summary>
        public void BlockTutorialInteraction()
        {
            RestoreHighlightedTarget();
            instructionPanel.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void HideTutorial()
        {
            RestoreHighlightedTarget();
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void HandleContinueClicked()
        {
            ContinueRequested?.Invoke();
        }

        private void HandleSkipClicked()
        {
            SkipRequested?.Invoke();
        }

        private void RestoreHighlightedTarget()
        {
            if (highlightTarget == null) return;
            highlightTarget.RestoreTutorialTarget();
            highlightTarget = null;
        }

        private void RefreshTutorialLayout()
        {
            highlightTarget.TargetRectTransform.GetWorldCorners(targetCorners);
            Vector2 targetMinimum = layoutRoot.InverseTransformPoint(targetCorners[0]);
            Vector2 targetMaximum = targetMinimum;
            foreach (Vector3 corner in targetCorners)
            {
                Vector2 localCorner = layoutRoot.InverseTransformPoint(corner);
                targetMinimum = Vector2.Min(targetMinimum, localCorner);
                targetMaximum = Vector2.Max(targetMaximum, localCorner);
            }
            Rect targetBounds = Rect.MinMaxRect(targetMinimum.x, targetMinimum.y, targetMaximum.x, targetMaximum.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layoutRoot, Screen.safeArea.min, uiCamera, out Vector2 safeMinimum);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layoutRoot, Screen.safeArea.max, uiCamera, out Vector2 safeMaximum);
            safeMinimum = Vector2.Max(safeMinimum, layoutRoot.rect.min);
            safeMaximum = Vector2.Min(safeMaximum, layoutRoot.rect.max);

            // Let Unity size the board to its contents.
            instructionPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, instructionWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(instructionPanel);
            Vector2 boardSize = instructionPanel.rect.size;
            float boardScale = Mathf.Min(1f, (safeMaximum.x - safeMinimum.x) / boardSize.x, (safeMaximum.y - safeMinimum.y) / boardSize.y);
            instructionPanel.localScale = Vector3.one * boardScale;
            boardSize *= boardScale;
            float gap = instructionLayoutGroup.spacing;

            // Try the four sides and the screen corners.
            boardPositions[0] = new Vector2(targetBounds.center.x - boardSize.x / 2f, targetBounds.yMin - gap - boardSize.y);
            boardPositions[1] = new Vector2(targetBounds.center.x - boardSize.x / 2f, targetBounds.yMax + gap);
            boardPositions[2] = new Vector2(targetBounds.xMax + gap, targetBounds.center.y - boardSize.y / 2f);
            boardPositions[3] = new Vector2(targetBounds.xMin - gap - boardSize.x, targetBounds.center.y - boardSize.y / 2f);
            boardPositions[4] = safeMinimum;
            boardPositions[5] = safeMaximum - boardSize;
            boardPositions[6] = new Vector2(safeMinimum.x, safeMaximum.y - boardSize.y);
            boardPositions[7] = new Vector2(safeMaximum.x - boardSize.x, safeMinimum.y);

            Vector2 boardPosition = safeMinimum;
            float leastOverlap = float.PositiveInfinity;
            float nearestDistance = float.PositiveInfinity;
            foreach (Vector2 position in boardPositions)
            {
                Rect candidateBounds = new Rect(Vector2.Min(Vector2.Max(position, safeMinimum), safeMaximum - boardSize), boardSize);
                Vector2 intersection = Vector2.Max(Vector2.zero, Vector2.Min(candidateBounds.max, targetBounds.max) - Vector2.Max(candidateBounds.min, targetBounds.min));
                float overlap = intersection.x * intersection.y;
                float distance = (candidateBounds.center - targetBounds.center).sqrMagnitude;
                if (overlap > leastOverlap || (Mathf.Approximately(overlap, leastOverlap) && distance >= nearestDistance)) continue;
                boardPosition = candidateBounds.min;
                leastOverlap = overlap;
                nearestDistance = distance;
            }
            instructionPanel.position = layoutRoot.TransformPoint(boardPosition + Vector2.Scale(boardSize, instructionPanel.pivot));
            if (leastOverlap > 0f && !hasWarnedAboutOverlap)
            {
                Debug.LogWarning("The tutorial board cannot avoid its target. Using the least overlapping position.", this);
                hasWarnedAboutOverlap = true;
            }
        }

        #endregion
    }
}
