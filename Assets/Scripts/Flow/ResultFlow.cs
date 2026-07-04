using System;
using DefaultNamespace.UI;
using UI;

namespace DefaultNamespace
{
    public class ResultFlow
    {
        private UIWinScreen winScreen;
        private UILoseScreen loseScreen;

        public event Action HomeRequested;
        public event Action RetryRequested;

        public void Enter(LevelSessionResult result)
        {
            if (result.DidWin)
            {
                winScreen = UIManager.Instance.ShowWinScreen();
                winScreen.HomeRequested += HandleHomeRequested;
                return;
            }

            loseScreen = UIManager.Instance.ShowLoseScreen(result.FailureMessage);
            loseScreen.RetryRequested += HandleRetryRequested;
            loseScreen.HomeRequested += HandleHomeRequested;
        }

        public void Exit()
        {
            if (winScreen != null)
                winScreen.HomeRequested -= HandleHomeRequested;
            if (loseScreen != null)
            {
                loseScreen.RetryRequested -= HandleRetryRequested;
                loseScreen.HomeRequested -= HandleHomeRequested;
            }

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
