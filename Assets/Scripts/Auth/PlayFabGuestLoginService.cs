using System;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;

namespace DefaultNamespace
{
    /// <summary>
    /// Service class used to log into PlayFab as a guest   
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
                var session = new PlayFabAuthSession(result.PlayFabId, guestCustomId, result.SessionTicket, result.NewlyCreated);
                completion.SetResult(session);
            }, error =>
            {
                completion.SetException(new InvalidOperationException($"PlayFab guest login failed: {error.GenerateErrorReport()}"));
            });

            return completion.Task;
        }
    }
}
