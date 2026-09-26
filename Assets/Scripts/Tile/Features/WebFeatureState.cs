using System;

namespace DefaultNamespace
{
    public class WebFeatureState : TileFeatureState
    {
        public override TileFeatureType FeatureType => TileFeatureType.Web;
        public int RemainingLayers { get; }

        public WebFeatureState(int remainingLayers)
        {
            if (remainingLayers <= 0) throw new ArgumentOutOfRangeException(nameof(remainingLayers), "A present web feature needs at least one layer.");
            RemainingLayers = remainingLayers;
        }

        protected override bool HasSameFeatureState(TileFeatureState tileFeatureState) => tileFeatureState is WebFeatureState webFeatureState && RemainingLayers == webFeatureState.RemainingLayers;
    }
}
