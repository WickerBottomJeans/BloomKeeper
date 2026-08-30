using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Owns the auth screen and prepares the current player account before notifying the application that it is ready
    /// </summary>
    public class AuthFlow
    {
        private const string GuestPlayButtonText = "<size=70%>Play as</size> <size=120%>Guest</size>";

        private readonly PlayFabGuestLoginService guestLoginService;
        private readonly PlayerSessionLoader playerSessionLoader;
        private readonly PlayerLivesPresentationService playerLivesPresentationService;
        private bool isAccountLoadInProgress;

        public event Action AccountReady;

        public AuthFlow(PlayFabGuestLoginService guestLoginService, PlayerSessionLoader playerSessionLoader, PlayerLivesPresentationService playerLivesPresentationService)
        {
            this.guestLoginService = guestLoginService;
            this.playerSessionLoader = playerSessionLoader;
            this.playerLivesPresentationService = playerLivesPresentationService;
        }

        /// <summary>
        /// [Duong] Really just showing AuthScreen UI
        /// </summary>
        public void Enter()
        {
            UIManager.Instance.AuthPlayRequested += HandleAuthPlayRequested;
            UIManager.Instance.ShowStartupScreen(GuestPlayButtonText, StartupScreenState.AccountEntry);
        }

        /// <summary>
        /// [Duong] Hide UI
        /// </summary>
        public void Exit()
        {
            UIManager.Instance.AuthPlayRequested -= HandleAuthPlayRequested;
            UIManager.Instance.HideStartupScreen();
        }

        private void HandleAuthPlayRequested()
        {
            LoadGuestAccount().Forget();
        }

        /// <summary>
        /// [Duong] Log in as guest then after succeeding, load account related data like progession, inventory, etc
        /// </summary>
        private async UniTask LoadGuestAccount()
        {
            if (isAccountLoadInProgress) return;

            isAccountLoadInProgress = true;
            (PlayerAccount account, PlayerLivesSnapshot livesSnapshot) playerSession;
            try
            {
                playerSession = await ApplicationPresentationService.Instance.RunWithLoading(async () =>
                {
                    PlayFabAuthSession playFabAuthSession = await guestLoginService.LoginAsGuest();
                    return await playerSessionLoader.Load(playFabAuthSession);
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                await DialogManager.Instance.RunOkDialog("Account unavailable", "Unable to load your account. Check your connection and try again.");
                return;
            }
            finally
            {
                isAccountLoadInProgress = false;
            }

            PlayerAccountContext.Instance.SetCurrentAccount(playerSession.account);
            playerLivesPresentationService.ReplaceServerLivesSnapshot(playerSession.livesSnapshot);
            AccountReady?.Invoke();
        }
    }
}
