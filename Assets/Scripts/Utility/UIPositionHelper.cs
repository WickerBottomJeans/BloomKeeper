using UI.Components;
using UnityEngine;

namespace Utility
{
    public static class UIPositionHelper
    {
        public static void ConvertWorldToCanvasAndClampPopupPosition(RectTransform panelRect, Canvas canvas, Vector2 screenPos, float padding = 10f)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            RectTransform parentRect = (RectTransform)panelRect.parent;
            Camera uiCamera = canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out Vector2 localPos);

            Vector3[] localCorners = new Vector3[4];
            panelRect.GetLocalCorners(localCorners);
            Vector2 minOffset = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maxOffset = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector3 canvasOffset3 = canvasRect.InverseTransformVector(panelRect.TransformVector(localCorners[i]));
                Vector2 canvasOffset = new Vector2(canvasOffset3.x, canvasOffset3.y);
                minOffset = Vector2.Min(minOffset, canvasOffset);
                maxOffset = Vector2.Max(maxOffset, canvasOffset);
            }

            bool placeRight = localPos.x < 0;

            Vector2 finalPos = localPos;

            if (placeRight)
                finalPos.x = localPos.x + padding - minOffset.x;
            else
                finalPos.x = localPos.x - padding - maxOffset.x;

            finalPos.x = Mathf.Clamp(finalPos.x, canvasRect.rect.xMin + padding - minOffset.x, canvasRect.rect.xMax - padding - maxOffset.x);
            finalPos.y = Mathf.Clamp(finalPos.y, canvasRect.rect.yMin + padding - minOffset.y, canvasRect.rect.yMax - padding - maxOffset.y);

            Vector3 worldPos = canvasRect.TransformPoint(finalPos);
            Vector3 parentLocalPos = parentRect.InverseTransformPoint(worldPos);
            panelRect.localPosition = new Vector3(parentLocalPos.x, parentLocalPos.y, panelRect.localPosition.z);
        }
    }
}
