using DefaultNamespace.UI;
using UnityEngine;

namespace Utility
{
    public static class BoardLayoutCalculator
    {
        public static BoardLayout Calculate(int cols, int rows, Camera cam, float paddingX, float paddingY, Rect playAreaScreenRect)
        {
            float cameraDistance = Mathf.Abs(cam.transform.position.z);
            Vector3 bottomLeft = cam.ScreenToWorldPoint(new Vector3(playAreaScreenRect.xMin, playAreaScreenRect.yMin, cameraDistance));
            Vector3 topRight = cam.ScreenToWorldPoint(new Vector3(playAreaScreenRect.xMax, playAreaScreenRect.yMax, cameraDistance));

            float availableWidth = (topRight.x - bottomLeft.x) * (1f - paddingX * 2f);
            float availableHeight = (topRight.y - bottomLeft.y) * (1f - paddingY * 2f);

            float tileSize    = Mathf.Min(availableWidth / cols, availableHeight / rows);
            float totalWidth  = cols * tileSize;
            float totalHeight = rows * tileSize;

            float centerX = (bottomLeft.x + topRight.x) * 0.5f;
            float centerY = (bottomLeft.y + topRight.y) * 0.5f;
            float originX = centerX - totalWidth / 2f + tileSize / 2f;
            float originY = centerY - totalHeight / 2f + tileSize / 2f;

            return new BoardLayout(tileSize, new Vector2(originX, originY), cols, rows);
        }
    }
}
