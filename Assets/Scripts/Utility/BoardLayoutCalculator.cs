using DefaultNamespace.UI;
using UnityEngine;

namespace Utility
{
    
    
    public static class BoardLayoutCalculator
    {
        //TODO: somethign arnt right here. bad feeling bout UIManager.Instance.GetScoreBoardHeight();
        public static BoardLayout Calculate(int cols, int rows, Camera cam, float paddingX, float paddingY)
        {
            Rect safeArea = Screen.safeArea;

            float scoreBoardHeightPx = UIManager.Instance.GetScoreBoardHeight();
            float scoreBoardWorldHeight = cam.ScreenToWorldPoint(new Vector3(0, scoreBoardHeightPx, 0)).y
                                          - cam.ScreenToWorldPoint(Vector3.zero).y;

            float screenWorldHeight = cam.orthographicSize * 2f;
            float screenWorldWidth  = screenWorldHeight * cam.aspect;

            float safeWidthRatio  = safeArea.width  / Screen.width;
            float safeHeightRatio = safeArea.height / Screen.height;

            float availableWidth  = screenWorldWidth  * safeWidthRatio  * (1f - paddingX * 2f);
            float availableHeight = screenWorldHeight * safeHeightRatio * (1f - paddingY * 2f) - scoreBoardWorldHeight;

            float cellSize    = Mathf.Min(availableWidth / cols, availableHeight / rows);
            float totalWidth  = cols * cellSize;
            float totalHeight = rows * cellSize;

            float worldBottomY = cam.ScreenToWorldPoint(Vector3.zero).y;
            float safeOffsetY  = screenWorldHeight * (1f - safeHeightRatio) / 2f;

            float originX = -totalWidth  / 2f + cellSize / 2f;
            float originY = worldBottomY + safeOffsetY + cellSize / 2f + paddingY;

            return new BoardLayout(cellSize, new Vector2(originX, originY));
        }
    }
}