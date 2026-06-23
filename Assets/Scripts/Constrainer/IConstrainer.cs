using System;

namespace DefaultNamespace
{
    public interface IConstrainer
    {
        event Action<ConstrainerFailureData> OnFailed;
        event Action OnProgressUpdated;
        ConstrainerType ConstrainerType { get; }
        ConstrainerViewData GetViewData();
    }

    public interface ITickableConstrainer
    {
        void Tick(float deltaTime);
    }
}
