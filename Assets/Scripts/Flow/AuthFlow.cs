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
        private readonly PlayerAccountLoader playerAccountLoader;
        private bool isAccountLoadInProgress;

        public event Action AccountReady;

        public AuthFlow(PlayFabGuestLoginService guestLoginService, PlayerAccountLoader playerAccountLoader)
        {
            this.guestLoginService = guestLoginService;
            this.playerAccountLoader = playerAccountLoader;
        }

        /// <summary>
        /// [Duong] Really just showing AuthScreen UI
        /// </summary>
        public void Enter()
        {
            UIManager.Instance.AuthPlayRequested += HandleAuthPlayRequested;
            UIManager.Instance.ShowAuthScreen(GuestPlayButtonText);
        }

        /// <summary>
        /// [Duong] Hide UI
        /// </summary>
        public void Exit()
        {
            UIManager.Instance.AuthPlayRequested -= HandleAuthPlayRequested;
            UIManager.Instance.HideAuthScreen();
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
            PlayerAccount playerAccount;
            try
            {
                playerAccount = await ApplicationPresentationService.Instance.RunWithLoading(async () =>
                {
                    PlayFabAuthSession playFabAuthSession = await guestLoginService.LoginAsGuest();
                    return await playerAccountLoader.Load(playFabAuthSession);
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                DialogOptionButton[] options = { DialogOptionButton.Ok };
                await DialogManager.Instance.RunDialogWorkflow("Account unavailable", "Unable to load your account. Check your connection and try again.", async dialogSession =>
                {
                    int buttonId = await dialogSession.WaitForButtonClick();
                    if ((DialogButtonType)buttonId != DialogButtonType.Ok) throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported account-load failure dialog button.");
                }, options);
                return;
            }
            finally
            {
                isAccountLoadInProgress = false;
            }

            PlayerAccountContext.Instance.SetCurrentAccount(playerAccount);
            AccountReady?.Invoke();
        }
    }
}
