namespace DefaultNamespace
{
    public abstract class TileFeatureState
    {
        public abstract TileFeatureType FeatureType { get; }
        protected abstract bool HasSameFeatureState(TileFeatureState tileFeatureState);

        public static bool AreFeatureStatesEqual(TileFeatureState firstFeatureState, TileFeatureState secondFeatureState)
        {
            if (ReferenceEquals(firstFeatureState, secondFeatureState)) return true;
            if (firstFeatureState == null || secondFeatureState == null) return false;
            if (firstFeatureState.FeatureType != secondFeatureState.FeatureType) return false;

            return firstFeatureState.HasSameFeatureState(secondFeatureState);
        }
    }
}
