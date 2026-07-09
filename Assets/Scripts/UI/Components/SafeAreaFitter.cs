using UnityEngine;

namespace DefaultNamespace.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake() => ApplySafeArea(true);
        private void OnEnable() => ApplySafeArea(true);
        private void Update() => ApplySafeArea(false);

        private void ApplySafeArea(bool force)
        {
            rectTransform ??= (RectTransform)transform;

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize) return;

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            if (!applyLeft) safeArea.xMin = 0f;
            if (!applyRight) safeArea.xMax = Screen.width;
            if (!applyBottom) safeArea.yMin = 0f;
            if (!applyTop) safeArea.yMax = Screen.height;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
