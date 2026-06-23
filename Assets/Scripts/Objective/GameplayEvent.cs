using System.Collections.Generic;

namespace DefaultNamespace
{
    public interface IGameplayEvent { }

    public class PetalsClearedEvent : IGameplayEvent
    {
        public List<PetalType> ClearedPetals { get; }

        public PetalsClearedEvent(List<PetalType> clearedPetals)
        {
            ClearedPetals = clearedPetals;
        }
    }

    public class SpiderWebClearedEvent : IGameplayEvent
    {
        public int CleanedTileCount { get; }

        public SpiderWebClearedEvent(int cleanedTileCount)
        {
            CleanedTileCount = cleanedTileCount;
        }
    }
}
