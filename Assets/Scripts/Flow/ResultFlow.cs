using System;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class ResultFlow
    {
        private readonly LevelCatalog levelCatalog;
        private int? currentLevelId;

        public event Action HomeRequested;
        public event Action RetryRequested;
        public event Action<int> NextLevelRequested;

        public ResultFlow(LevelCatalog levelCatalog)
        {
            this.levelCatalog = levelCatalog ?? throw new ArgumentNullException(nameof(levelCatalog));
        }

        public void Enter(LevelSessionResult result)
        {
            if (result.DidWin)
            {
                currentLevelId = result.LevelId;
                bool showNext = TryResolveNextLevel(result.LevelId, out _);
                UIManager.Instance.ShowWinScreen(result.Stars, result.StarCap, showNext);
                UIManager.Instance.WinScreenHomeRequested += HandleHomeRequested;
                if (showNext)
                    UIManager.Instance.WinScreenNextRequested += HandleNextRequested;
                return;
            }

            UIManager.Instance.ShowLoseScreen(result.FailureMessage);
            UIManager.Instance.LoseScreenRetryRequested += HandleRetryRequested;
            UIManager.Instance.LoseScreenHomeRequested += HandleHomeRequested;
        }

        public void Exit()
        {
            UIManager.Instance.WinScreenHomeRequested -= HandleHomeRequested;
            UIManager.Instance.WinScreenNextRequested -= HandleNextRequested;
            UIManager.Instance.LoseScreenRetryRequested -= HandleRetryRequested;
            UIManager.Instance.LoseScreenHomeRequested -= HandleHomeRequested;

            UIManager.Instance.HideWinScreen();
            UIManager.Instance.HideLoseScreen();
            currentLevelId = null;
        }

        private bool TryResolveNextLevel(int levelId, out int nextLevelId)
        {
            if (!levelCatalog.TryGetNextLevelId(levelId, out nextLevelId)) return false;

            if (nextLevelId > PlayerAccountContext.Instance.GetCurrentProgression().highestUnlockedLevel)
                throw new InvalidOperationException($"Level {nextLevelId} follows completed level {levelId} but is not unlocked by confirmed progression.");

            return true;
        }

        private void HandleRetryRequested()
        {
            RetryRequested?.Invoke();
        }

        private void HandleHomeRequested()
        {
            HomeRequested?.Invoke();
        }

        private void HandleNextRequested()
        {
            if (!currentLevelId.HasValue)
                throw new InvalidOperationException("Cannot request the next level without an active winning result.");
            if (!TryResolveNextLevel(currentLevelId.Value, out int nextLevelId))
                throw new InvalidOperationException($"Cannot request a level after final level {currentLevelId.Value}.");

            NextLevelRequested?.Invoke(nextLevelId);
        }
    }
}
