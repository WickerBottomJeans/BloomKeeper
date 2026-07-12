using System;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    /// <summary>
    /// Owns the auth screen before the player enters home
    /// </summary>
    public class AuthFlow
    {
        private const string GuestPlayButtonText = "Play as guest";

        private readonly PlayFabGuestLoginService guestLoginService;
        private bool isLoginInProgress;

        public event Action<PlayFabAuthSession> AuthCompleted;
        public event Action<Exception> AuthFailed;

        public AuthFlow(PlayFabGuestLoginService guestLoginService)
        {
            this.guestLoginService = guestLoginService;
        }

        public void Enter()
        {
            UIManager.Instance.AuthPlayRequested += HandleAuthPlayRequested;
            UIManager.Instance.ShowAuthScreen(GuestPlayButtonText);
        }

        public void Exit()
        {
            UIManager.Instance.AuthPlayRequested -= HandleAuthPlayRequested;
            UIManager.Instance.HideAuthScreen();
        }

        private async void HandleAuthPlayRequested()
        {
            if (isLoginInProgress) return;

            isLoginInProgress = true;
            try
            {
                PlayFabAuthSession session = await guestLoginService.LoginAsGuest();
                AuthCompleted?.Invoke(session);
            }
            catch (Exception exception)
            {
                AuthFailed?.Invoke(exception);
            }
            finally
            {
                isLoginInProgress = false;
            }
        }
    }
}
