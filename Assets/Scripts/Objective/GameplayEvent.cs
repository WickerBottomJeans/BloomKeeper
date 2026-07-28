using System;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public interface IGameplayEvent { }

    public interface IGameplayEventHandler
    {
        Type HandledEventType { get; }
        void Handle(IGameplayEvent e);
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
