using UI.Components;
using UnityEngine;

namespace Utility
{
    public static class UIPositionHelper
    {
        public static void ConvertWorldToCanvasAndClampPopupPosition(RectTransform panelRect, Canvas canvas, Vector2 screenPos, float padding = 10f)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (panelRect.parent != canvasRect)
            {
                Debug.LogError("[UIPositionHelper] PanelRect must be a direct child of the canvas.");
                return;
            }
            Camera uiCamera = canvas.worldCamera;

            Vector2 popupSize = panelRect.rect.size;
            Vector2 canvasSize = canvasRect.rect.size;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out Vector2 localPos);

            float halfCW = canvasSize.x * 0.5f;
            float halfCH = canvasSize.y * 0.5f;

            bool placeRight = localPos.x < 0;

            Vector2 finalPos = localPos;

            if (placeRight)
                finalPos.x = localPos.x + popupSize.x + padding;
            else
                finalPos.x = localPos.x - popupSize.x - padding;

            finalPos.x = Mathf.Clamp(finalPos.x, -halfCW + popupSize.x * 0.5f + padding, halfCW - popupSize.x * 0.5f - padding);
            finalPos.y = Mathf.Clamp(finalPos.y, -halfCH + popupSize.y * 0.5f + padding, halfCH - popupSize.y * 0.5f - padding);

            panelRect.localPosition = finalPos;
        }
    }
}