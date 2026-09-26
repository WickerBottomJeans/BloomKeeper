namespace DefaultNamespace
{
    public abstract class TileFeature
    {
        public abstract TileFeatureType FeatureType { get; }
        public abstract bool AllowsPetals { get; }
        public abstract bool AllowsGravity { get; }
        public abstract bool IsRemoved { get; }

        public abstract int GetFeatureClearEffectCapacity();

        /// <summary>
        /// Applies a clear effect and returns whether the feature consumed it.
        /// </summary>
        public abstract bool TryAbsorbClearEffect();

        public abstract void HandleAdjacentTileMatched();
        public abstract TileFeatureState CaptureFeatureState();
    }
}
