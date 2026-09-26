using UnityEngine;

namespace DefaultNamespace
{
    public readonly struct TileState
    {
        public Vector2Int Position { get; }
        public bool IsVoid { get; }
        public bool IsPlayable { get; }
        public PetalType? PetalType { get; }
        public SpecialSkillType SkillType { get; }
        public TileFeatureState FeatureState { get; }
        public bool CanClearPetal { get; }

        public TileState(Vector2Int position, bool isVoid, bool isPlayable, PetalType? petalType, SpecialSkillType skillType, TileFeatureState featureState, bool canClearPetal)
        {
            Position = position;
            IsVoid = isVoid;
            IsPlayable = isPlayable;
            PetalType = petalType;
            SkillType = skillType;
            FeatureState = featureState;
            CanClearPetal = canClearPetal;
        }
    }
}
