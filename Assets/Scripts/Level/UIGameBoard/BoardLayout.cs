using UnityEngine;

namespace DefaultNamespace.UI
{
    public class BoardLayout
    {
        /// <summary>
        /// Size of a single square cell in world units.
        /// </summary>
        public readonly float CellSize;
        public readonly Vector2 OriginWorldPos;
        public readonly int Cols;
        public readonly int Rows;

        public BoardLayout(float cellSize, Vector2 originWorldPos, int cols, int rows)
        {
            CellSize = cellSize;
            OriginWorldPos = originWorldPos;
            Cols = cols;
            Rows = rows;
        }

        public Vector2 GetCellWorldPos(int x, int y) =>
            OriginWorldPos + new Vector2(x * CellSize, y * CellSize);
    }
}