using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Fits child content inside the safe area and optionally reserves space for screen insets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaContentFitter : MonoBehaviour, ILayoutGroup, ILayoutElement
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        [SerializeField] private bool reserveTopInset;
        [SerializeField] private bool reserveBottomInset;
        [SerializeField] private float contentHeight;

        private readonly Vector3[] panelWorldCorners = new Vector3[4];
        private RectTransform panelRectTransform;
        private Canvas layoutCanvas;
        private Rect lastPanelScreenRect;
        private Rect lastSafeArea;
        private bool isInitialized;
        private bool hasLayoutSnapshot;
        private float reservedPanelHeight = -1f;

        #region Unity Lifecycle

        private void OnEnable()
        {
            hasLayoutSnapshot = false;
            RequestContentLayout();
        }

        private void LateUpdate()
        {
            if (!isInitialized) return;
            Rect panelScreenRect = CalculatePanelScreenRect();
            Rect safeArea = Screen.safeArea;
            float requiredPanelHeight = CalculateReservedPanelHeight();
            if (hasLayoutSnapshot && panelScreenRect == lastPanelScreenRect && safeArea == lastSafeArea && requiredPanelHeight == reservedPanelHeight) return;
            RequestContentLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            RequestContentLayout();
        }

        private void OnTransformParentChanged()
        {
            RequestContentLayout();
        }

        private void OnValidate()
        {
            hasLayoutSnapshot = false;
            RequestContentLayout();
        }

        #endregion

        #region Public API

        // Leave width sizing to the other layout components.
        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => reservedPanelHeight;
        public float preferredHeight => reservedPanelHeight;
        public float flexibleHeight
        {
            get
            {
                if (reserveTopInset || reserveBottomInset) return 0f;
                return -1f;
            }
        }

        // Override the authored LayoutElement height when reserving inset space.
        public int layoutPriority => 2;

        public void CalculateLayoutInputHorizontal() { }

        public void CalculateLayoutInputVertical()
        {
            reservedPanelHeight = CalculateReservedPanelHeight();
        }

        /// <summary>
        /// Supplies the runtime Canvas and schedules content fitting when enabled.
        /// </summary>
        public void InitializeSafeAreaContent(Canvas uiCanvas)
        {
            // Validate the Canvas argument.
            if (uiCanvas == null) throw new ArgumentNullException(nameof(uiCanvas));

            // Cache layout dependencies.
            panelRectTransform = GetComponent<RectTransform>();
            layoutCanvas = uiCanvas.rootCanvas;

            // Validate the content and Canvas setup.
            if (content.parent != panelRectTransform) throw new InvalidOperationException("Safe area content must be a direct child of its fitter.");
            if (layoutCanvas.renderMode == RenderMode.WorldSpace) throw new InvalidOperationException("Safe area content requires a screen space Canvas.");
            if (!transform.IsChildOf(layoutCanvas.transform)) throw new InvalidOperationException("Safe area content must belong to the supplied Canvas hierarchy.");
            if ((reserveTopInset || reserveBottomInset) && (contentHeight <= 0f || float.IsNaN(contentHeight) || float.IsInfinity(contentHeight))) throw new InvalidOperationException("Reserving screen insets requires a positive, finite content height.");

            // Initialize content fitting.
            isInitialized = true;
            hasLayoutSnapshot = false;
            RequestContentLayout();
        }

        public void SetLayoutHorizontal()
        {
            ApplyContentSafeArea(0);
        }

        public void SetLayoutVertical()
        {
            ApplyContentSafeArea(1);
        }

        #endregion

        #region Private Methods

        private float CalculateReservedPanelHeight()
        {
            // Supply no height until initialized or when only fitting child content.
            if (!isInitialized || (!reserveTopInset && !reserveBottomInset)) return -1f;

            // Add the selected screen margins in pixels.
            Rect safeArea = Screen.safeArea;
            float insetPixels = 0f;
            if (reserveTopInset) insetPixels += Screen.height - safeArea.yMax;
            if (reserveBottomInset) insetPixels += safeArea.yMin;

            // Convert the pixel distance to this panel's local UI units.
            Camera canvasCamera = null;
            if (layoutCanvas.renderMode == RenderMode.ScreenSpaceCamera) canvasCamera = layoutCanvas.worldCamera;
            Vector2 screenStart = new Vector2(Screen.width * 0.5f, 0f);
            Vector2 screenEnd = new Vector2(screenStart.x, insetPixels);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, screenStart, canvasCamera, out Vector2 localStart) || !RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, screenEnd, canvasCamera, out Vector2 localEnd)) throw new InvalidOperationException("The screen inset cannot be projected onto the panel.");
            float insetHeight = localEnd.y - localStart.y;
            if (insetHeight < 0f) throw new InvalidOperationException("Reserving screen insets requires an upright panel.");

            // Preserve the authored content height and reserve extra space for the inset.
            return contentHeight + insetHeight;
        }

        private void RequestContentLayout()
        {
            if (!isInitialized || !isActiveAndEnabled || CanvasUpdateRegistry.IsRebuildingLayout()) return;
            LayoutRebuilder.MarkLayoutForRebuild(panelRectTransform);
        }

        private void ApplyContentSafeArea(int axis)
        {
            // Wait for the owning view to supply the Canvas.
            if (!isInitialized || !isActiveAndEnabled) return;

            Rect panelScreenRect = CalculatePanelScreenRect();
            Rect safeArea = Screen.safeArea;
            
            // If working horizontally, read the left and right edges.
            // If working vertically, read the bottom and top edges.
            float panelStartEdge = panelScreenRect.min[axis];
            float panelEndEdge = panelScreenRect.max[axis];
            float panelLength = panelEndEdge - panelStartEdge;

            // If working horizontally, use the left and right safe-area settings.
            bool constrainStartEdge = axis == 0 ? applyLeft : applyBottom;
            // If working vertically, use the bottom and top safe-area settings.
            bool constrainEndEdge = axis == 0 ? applyRight : applyTop;
            
            float contentStartEdgePosition = constrainStartEdge  ? Mathf.Max(panelStartEdge , safeArea.min[axis]) : panelStartEdge ;
            float contentEndEdgePosition = constrainEndEdge  ? Mathf.Min(panelEndEdge , safeArea.max[axis]) : panelEndEdge ;
            if (panelLength  > 0f && contentEndEdgePosition <= contentStartEdgePosition) throw new InvalidOperationException($"Safe area content on '{name}' has no usable space on axis {axis}.");

            // Copy the current anchors and offsets so the other direction stays unchanged.
            Vector2 contentAnchorMin = content.anchorMin;
            Vector2 contentAnchorMax = content.anchorMax;
            Vector2 contentOffsetMin = content.offsetMin;
            Vector2 contentOffsetMax = content.offsetMax;
            
            if (panelLength == 0f)
            {
                // Span the zero-sized panel without dividing by zero.
                contentAnchorMin[axis] = 0f;
                contentAnchorMax[axis] = 1f;
            }
            else
            {
                // Convert screen edge positions to anchors relative to the panel.
                contentAnchorMin[axis] = (contentStartEdgePosition - panelStartEdge) / panelLength;
                contentAnchorMax[axis] = (contentEndEdgePosition - panelStartEdge) / panelLength;
            }
            
            // Remove offsets so the content edges sit exactly at the calculated anchors.
            contentOffsetMin[axis] = 0f;
            contentOffsetMax[axis] = 0f;

            // Write changed anchors and offsets back to the content RectTransform.
            if (content.anchorMin != contentAnchorMin) content.anchorMin = contentAnchorMin;
            if (content.anchorMax != contentAnchorMax) content.anchorMax = contentAnchorMax;
            if (content.offsetMin != contentOffsetMin) content.offsetMin = contentOffsetMin;
            if (content.offsetMax != contentOffsetMax) content.offsetMax = contentOffsetMax;

            // Copy the saved rectangles so the other direction stays unchanged.
            Vector2 panelPosition = lastPanelScreenRect.position;
            Vector2 panelDimensions = lastPanelScreenRect.size;
            Vector2 safeAreaPosition = lastSafeArea.position;
            Vector2 safeAreaDimensions = lastSafeArea.size;

            // Update horizontal positions and widths if working horizontally, or vertical positions and heights if working vertically.
            panelPosition[axis] = panelScreenRect.position[axis];
            panelDimensions[axis] = panelScreenRect.size[axis];
            safeAreaPosition[axis] = safeArea.position[axis];
            safeAreaDimensions[axis] = safeArea.size[axis];

            // Save the updated rectangles for the next change check.
            lastPanelScreenRect = new Rect(panelPosition, panelDimensions);
            lastSafeArea = new Rect(safeAreaPosition, safeAreaDimensions);

            // Mark the saved bounds ready after the vertical layout pass.
            if (axis == 1) hasLayoutSnapshot = true;
        }

        private Rect CalculatePanelScreenRect()
        {
            // Get the panel corners in screen pixels.
            panelRectTransform.GetWorldCorners(panelWorldCorners);
            Vector2 bottomLeft = ProjectPanelCorner(panelWorldCorners[0]);
            Vector2 topLeft = ProjectPanelCorner(panelWorldCorners[1]);
            Vector2 topRight = ProjectPanelCorner(panelWorldCorners[2]);
            Vector2 bottomRight = ProjectPanelCorner(panelWorldCorners[3]);

            // Reject rotated or mirrored screen bounds.
            bool axisAligned = Mathf.Approximately(bottomLeft.x, topLeft.x) && Mathf.Approximately(topLeft.y, topRight.y) && Mathf.Approximately(topRight.x, bottomRight.x) && Mathf.Approximately(bottomRight.y, bottomLeft.y);
            if (!axisAligned || bottomLeft.x > topRight.x || bottomLeft.y > topRight.y) throw new InvalidOperationException($"Safe area content on '{name}' requires a screen aligned panel without mirroring.");

            // Build the panel's screen rectangle.
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        private Vector2 ProjectPanelCorner(Vector3 worldCorner)
        {
            switch (layoutCanvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    return worldCorner;
                case RenderMode.ScreenSpaceCamera:
                    Vector3 screenCorner = layoutCanvas.worldCamera.WorldToScreenPoint(worldCorner);
                    if (screenCorner.z <= 0f) throw new InvalidOperationException("Safe area content must be in front of its Canvas camera.");
                    return screenCorner;
                default:
                    throw new InvalidOperationException("Safe area content requires a screen space Canvas.");
            }
        }

        #endregion
    }
}
