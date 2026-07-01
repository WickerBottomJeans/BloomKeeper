using System.Collections.Generic;

using DefaultNamespace.UI;

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

    public class PlayerMoveCommittedEvent : IGameplayEvent { }

    public class BoardResolvedEvent : IGameplayEvent
    {
        public MatchResolveResult Result { get; }
        public int CascadeDepth { get; }
        public bool CountsForScore { get; }

        public BoardResolvedEvent(MatchResolveResult result, int cascadeDepth, bool countsForScore)
        {
            Result = result;
            CascadeDepth = cascadeDepth;
            CountsForScore = countsForScore;
        }
    }
}
