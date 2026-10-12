namespace DefaultNamespace
{
    public sealed class BasketFeatureState : TileFeatureState
    {
        public static BasketFeatureState Instance { get; } = new();
        private BasketFeatureState() { }
        public override TileFeatureType FeatureType => TileFeatureType.Basket;
        protected override bool HasSameFeatureState(TileFeatureState tileFeatureState) => tileFeatureState is BasketFeatureState;
    }
}
