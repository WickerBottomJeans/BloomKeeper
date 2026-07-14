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

            PlayFabClientAPI.LoginWithCustomID(request, result =>
            {
                if (result.EntityToken != null)
                {
                    CompleteWithSession(result, guestCustomId, result.EntityToken.Entity?.Id, result.EntityToken.Entity?.Type, result.EntityToken.EntityToken, result.EntityToken.TokenExpiration, completion);
                    return;
                }

                CompleteWithEntityToken(result, guestCustomId, completion);
            }, error =>
            {
                completion.SetException(new InvalidOperationException($"PlayFab guest login failed: {error.GenerateErrorReport()}"));
            });

            return completion.Task;
        }

        /// <summary>
        /// Use this when login worked, but entity auth is missing so now this func gon it, then finish login 
        /// </summary>
        /// <param name="loginResult"></param>
        /// <param name="guestCustomId"></param>
        /// <param name="completion"></param>
        private static void CompleteWithEntityToken(LoginResult loginResult, string guestCustomId, TaskCompletionSource<PlayFabAuthSession> completion)
        {
            PlayFabAuthenticationAPI.GetEntityToken(new GetEntityTokenRequest(), result =>
            {
                CompleteWithSession(loginResult, guestCustomId, result.Entity?.Id, result.Entity?.Type, result.EntityToken, result.TokenExpiration, completion);
            }, error =>
            {
                completion.SetException(new InvalidOperationException($"PlayFab entity token request failed after guest login: {error.GenerateErrorReport()}"));
            });
        }

        private static void CompleteWithSession(LoginResult loginResult, string guestCustomId, string entityId, string entityType, string entityToken, DateTime? entityTokenExpiration, TaskCompletionSource<PlayFabAuthSession> completion)
        {
            try
            {
                completion.SetResult(CreateSession(loginResult, guestCustomId, entityId, entityType, entityToken, entityTokenExpiration));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }

        private static PlayFabAuthSession CreateSession(LoginResult loginResult, string guestCustomId, string entityId, string entityType, string entityToken, DateTime? entityTokenExpiration)
        {
            if (string.IsNullOrWhiteSpace(entityId)) throw new InvalidOperationException("PlayFab guest login succeeded without an entity ID.");
            if (string.IsNullOrWhiteSpace(entityType)) throw new InvalidOperationException("PlayFab guest login succeeded without an entity type.");
            if (string.IsNullOrWhiteSpace(entityToken)) throw new InvalidOperationException("PlayFab guest login succeeded without an entity token.");

            return new PlayFabAuthSession(loginResult.PlayFabId, guestCustomId, loginResult.SessionTicket, entityId, entityType, entityToken, entityTokenExpiration, loginResult.NewlyCreated);
        }
    }
}
