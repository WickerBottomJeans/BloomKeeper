namespace DefaultNamespace
{
    public sealed class BasketFeature : TileFeature
    {
        public override TileFeatureType FeatureType => TileFeatureType.Basket;
        public override bool AllowsPieces => false;
        public override bool AllowsGravity => false;
        public override bool IsRemoved => false;
        public override int GetFeatureClearEffectCapacity() => 0;
        public override bool TryAbsorbClearEffect() => true;
        public override void HandleAdjacentTileMatched() { }
        public override TileFeatureState CaptureFeatureState() => BasketFeatureState.Instance;
    }
}
