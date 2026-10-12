using UnityEngine;

namespace DefaultNamespace
{
    public readonly struct TileState
    {
        public Vector2Int Position { get; }
        public bool IsVoid { get; }
        public PetalType? PetalType { get; }
        public SpecialSkillType SkillType { get; }
        public TileFeatureState FeatureState { get; }
        public bool CanClearPetal { get; }
        public bool HasEgg { get; }

        public TileState(Vector2Int position, bool isVoid, PetalType? petalType, SpecialSkillType skillType, TileFeatureState featureState, bool canClearPetal, bool hasEgg = false)
        {
            Position = position;
            IsVoid = isVoid;
            PetalType = petalType;
            SkillType = skillType;
            FeatureState = featureState;
            CanClearPetal = canClearPetal;
            HasEgg = hasEgg;
        }
    }
}
