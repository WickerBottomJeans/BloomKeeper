using System;

namespace DefaultNamespace
{
    public class WebFeature : TileFeature
    {
        public const int MaxLayers = 5;

        public override TileFeatureType FeatureType => TileFeatureType.Web;
        public int RemainingLayers { get; private set; }
        public override bool IsRemoved => RemainingLayers == 0;
        public override bool AllowsPetals => IsRemoved;
        public override bool AllowsGravity => IsRemoved;

        public WebFeature(int webLayers)
        {
            if (webLayers <= 0) throw new ArgumentOutOfRangeException(nameof(webLayers), "A web feature needs at least one layer.");
            if (webLayers > MaxLayers) throw new ArgumentOutOfRangeException(nameof(webLayers), $"A web feature supports at most {MaxLayers} layers.");
            RemainingLayers = webLayers;
        }

        public override int GetFeatureClearEffectCapacity() => RemainingLayers;
        public override bool TryAbsorbClearEffect() => TryReduceWebLayer();
        public override void HandleAdjacentTileMatched() => TryReduceWebLayer();
        public override TileFeatureState CaptureFeatureState() => new WebFeatureState(RemainingLayers);

        private bool TryReduceWebLayer()
        {
            if (IsRemoved) return false;

            RemainingLayers--;
            return true;
        }
    }
}
