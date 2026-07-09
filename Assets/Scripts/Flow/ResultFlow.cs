using System;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class ResultFlow
    {
        public event Action HomeRequested;
        public event Action RetryRequested;

        public void Enter(LevelSessionResult result)
        {
            if (result.DidWin)
            {
                UIManager.Instance.ShowWinScreen(result.Stars, result.StarCap);
                UIManager.Instance.WinScreenHomeRequested += HandleHomeRequested;
                return;
            }

            UIManager.Instance.ShowLoseScreen(result.FailureMessage);
            UIManager.Instance.LoseScreenRetryRequested += HandleRetryRequested;
            UIManager.Instance.LoseScreenHomeRequested += HandleHomeRequested;
        }

        public void Exit()
        {
            UIManager.Instance.WinScreenHomeRequested -= HandleHomeRequested;
            UIManager.Instance.LoseScreenRetryRequested -= HandleRetryRequested;
            UIManager.Instance.LoseScreenHomeRequested -= HandleHomeRequested;

            UIManager.Instance.HideWinScreen();
            UIManager.Instance.HideLoseScreen();
        }

        private void HandleRetryRequested()
        {
            RetryRequested?.Invoke();
        }

        private void HandleHomeRequested()
        {
            HomeRequested?.Invoke();
        }
    }
}
