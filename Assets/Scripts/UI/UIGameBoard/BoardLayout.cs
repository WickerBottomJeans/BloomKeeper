using UnityEngine;

namespace DefaultNamespace.UI
{
    public readonly struct BoardLayout
    {
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
    }
}