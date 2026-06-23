namespace DefaultNamespace
{
    public readonly struct TileImpactResult
    {
        public Petal RemovedPetal { get; }
        public bool TileChanged { get; }
        //TODO: Coupling here. i knew it, couldnt find a way to isolate it out tho
        public bool SpiderWebCleaned { get; }

        public TileImpactResult(Petal removedPetal, bool tileChanged, bool spiderWebCleaned = false)
        {
            RemovedPetal = removedPetal;
            TileChanged = tileChanged;
            SpiderWebCleaned = spiderWebCleaned;
        }
    }
}
