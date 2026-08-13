using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public interface IGameplayEvent { }

    public interface IGameplayEventHandler<in TEvent> where TEvent : IGameplayEvent
    {
        void Handle(TEvent gameplayEvent);
    }

    public class PlayerMoveCommittedEvent : IGameplayEvent { }

    public class BoardResolutionStepCompletedEvent : IGameplayEvent
    {
        public MatchResolveResult Result { get; }
        public int CascadeDepth { get; }
        public bool IsFromPlayerMove { get; }

        public BoardResolutionStepCompletedEvent(MatchResolveResult result, int cascadeDepth, bool isFromPlayerMove)
        {
            Result = result;
            CascadeDepth = cascadeDepth;
            IsFromPlayerMove = isFromPlayerMove;
        }
    }
}
