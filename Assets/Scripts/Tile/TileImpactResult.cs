namespace DefaultNamespace
{
    public readonly struct TileImpactResult
    {
        public Petal RemovedPetal { get; }
        public bool TileChanged { get; }

        public TileImpactResult(Petal removedPetal, bool tileChanged)
        {
            RemovedPetal = removedPetal;
            TileChanged = tileChanged;
        }
    }
}
