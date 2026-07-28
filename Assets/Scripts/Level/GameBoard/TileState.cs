using UnityEngine;

namespace DefaultNamespace
{
    public readonly struct TileState
    {
        public Vector2Int Position { get; }
        public bool IsVoid { get; }
        public TileType? TileType { get; }
        public PetalType? PetalType { get; }
        public SpecialSkillType SkillType { get; }
        public int ObstacleLayerCount { get; }
        public bool CanClearPetal { get; }

        public TileState(Vector2Int position, bool isVoid, TileType? tileType, PetalType? petalType, SpecialSkillType skillType, int obstacleLayerCount, bool canClearPetal)
        {
            Position = position;
            IsVoid = isVoid;
            TileType = tileType;
            PetalType = petalType;
            SkillType = skillType;
            ObstacleLayerCount = obstacleLayerCount;
            CanClearPetal = canClearPetal;
        }
    }
}
