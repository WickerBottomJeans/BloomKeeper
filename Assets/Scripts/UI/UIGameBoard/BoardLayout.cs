using UnityEngine;

namespace DefaultNamespace.UI
{
    public readonly struct BoardLayout
    {
        public readonly float CellSize;
        public readonly Vector2 OriginWorldPos;

        public BoardLayout(float cellSize, Vector2 originWorldPos)
        {
            CellSize = cellSize;
            OriginWorldPos = originWorldPos;
        }
    }
}