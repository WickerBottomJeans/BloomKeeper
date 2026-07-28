using UnityEngine;

namespace DefaultNamespace.UI
{
    public class BoardLayout
    {
        /// <summary>
        /// Size of a single square tile in world units.
        /// </summary>
        public readonly float TileSize;
        public readonly Vector2 OriginWorldPos;
        public readonly int Cols;
        public readonly int Rows;

        public BoardLayout(float tileSize, Vector2 originWorldPos, int cols, int rows)
        {
            TileSize = tileSize;
            OriginWorldPos = originWorldPos;
            Cols = cols;
            Rows = rows;
        }

        public Vector2 GetTileWorldPos(int x, int y) =>
            OriginWorldPos + new Vector2(x * TileSize, y * TileSize);
    }
}