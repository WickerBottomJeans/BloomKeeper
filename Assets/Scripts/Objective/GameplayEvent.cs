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
        public BoardResolution Result { get; }
        public int CascadeDepth { get; }
        public bool IsPlayerInitiated { get; }

        public BoardResolutionStepCompletedEvent(BoardResolution result, int cascadeDepth, bool isPlayerInitiated)
        {
            Result = result;
            CascadeDepth = cascadeDepth;
            IsPlayerInitiated = isPlayerInitiated;
        }
    }
}
