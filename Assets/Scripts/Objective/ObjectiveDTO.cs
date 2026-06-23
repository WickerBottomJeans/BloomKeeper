using System.Collections.Generic;

namespace DefaultNamespace
{
    public interface IObjectiveEvent { }

    public class PetalsClearedEvent : IObjectiveEvent
    {
        public List<PetalType> ClearedPetals { get; }

        public PetalsClearedEvent(List<PetalType> clearedPetals)
        {
            ClearedPetals = clearedPetals;
        }
    }

    public class SpiderWebClearedEvent : IObjectiveEvent
    {
        public int CleanedTileCount { get; }

        public SpiderWebClearedEvent(int cleanedTileCount)
        {
            CleanedTileCount = cleanedTileCount;
        }
    }
}
