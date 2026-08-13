using System;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.AuthenticationModels;
using PlayFab.ClientModels;

namespace DefaultNamespace
{
    /// <summary>
    /// log into PlayFab as guest and prepare entity auth for online/server calls
    /// </summary>
    public class PlayFabGuestLoginService
    {
        private readonly GuestCustomIdStore guestCustomIdStore;

        public PlayFabGuestLoginService(GuestCustomIdStore guestCustomIdStore)
        {
            this.guestCustomIdStore = guestCustomIdStore;
        }

        public Task<PlayFabAuthSession> LoginAsGuest()
        {
            string guestCustomId = guestCustomIdStore.GetOrCreateGuestCustomId();
            var completion = new TaskCompletionSource<PlayFabAuthSession>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new LoginWithCustomIDRequest { CustomId = guestCustomId, CreateAccount = true };

            PlayFabClientAPI.LoginWithCustomID(request, loginResult =>
            {
                PlayFabAuthenticationAPI.GetEntityToken(new GetEntityTokenRequest(), entityTokenResult =>
                {
                    try
                    {
                        completion.SetResult(new PlayFabAuthSession(loginResult.PlayFabId, guestCustomId, loginResult.SessionTicket, entityTokenResult.Entity?.Id, entityTokenResult.Entity?.Type, entityTokenResult.EntityToken, entityTokenResult.TokenExpiration, loginResult.NewlyCreated));
                    }
                    catch (Exception exception)
                    {
                        completion.SetException(exception);
                    }
                }, error =>
                {
                    completion.SetException(new InvalidOperationException($"PlayFab entity token request failed after guest login: {error.GenerateErrorReport()}"));
                });
            }, error =>
            {
                completion.SetException(new InvalidOperationException($"PlayFab guest login failed: {error.GenerateErrorReport()}"));
            });

            return completion.Task;
        }

    }
}
