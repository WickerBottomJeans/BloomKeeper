using UnityEngine;

namespace DefaultNamespace.UI
{
    public class BoardLayout
    {
        /// <summary>
        /// Size of a single square tile in world units.
        /// </summary>
        public readonly float TileSize;
        public readonly Vector2 OriginWorldPosition;
        public readonly int ColumnCount;
        public readonly int RowCount;

        public BoardLayout(float tileSize, Vector2 originWorldPosition, int columnCount, int rowCount)
        {
            TileSize = tileSize;
            OriginWorldPosition = originWorldPosition;
            ColumnCount = columnCount;
            RowCount = rowCount;
        }

        public Vector2 GetTileWorldPosition(int x, int y) =>
            OriginWorldPosition + new Vector2(x * TileSize, y * TileSize);
    }
}