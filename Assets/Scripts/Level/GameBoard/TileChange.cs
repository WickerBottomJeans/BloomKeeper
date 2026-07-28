using UnityEngine;

namespace DefaultNamespace
{
    public readonly struct TileChange
    {
        public TileState Before { get; }
        public TileState After { get; }
        public Vector2Int Position => Before.Position;
        public bool PetalChanged => Before.PetalType != After.PetalType || Before.SkillType != After.SkillType;
        public bool TileTypeChanged => Before.TileType != After.TileType;
        public bool ObstacleLayerChanged => Before.ObstacleLayerCount != After.ObstacleLayerCount;
        public bool HasAnyChange => Before.IsVoid != After.IsVoid || PetalChanged || TileTypeChanged || ObstacleLayerChanged;
        public bool PetalWasRemoved => Before.PetalType.HasValue && !After.PetalType.HasValue;
        public PetalType RemovedPetalType => PetalWasRemoved ? Before.PetalType.Value : PetalType.None;
        public SpecialSkillType RemovedSkillType => PetalWasRemoved ? Before.SkillType : SpecialSkillType.None;
        public bool ObstacleWasCleared => Before.ObstacleLayerCount > 0 && After.ObstacleLayerCount == 0;

        public TileChange(TileState before, TileState after)
        {
            if (before.Position != after.Position)
                throw new System.ArgumentException("A board tile change must describe the same position before and after.");

            Before = before;
            After = after;
        }
    }
}
