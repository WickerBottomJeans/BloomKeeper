namespace DefaultNamespace
{
    /// <summary>
    /// Represents the PlayFab auth state for this game session
    /// </summary>
    public class PlayFabAuthSession
    {
        public string PlayFabId { get; }
        public string GuestCustomId { get; }
        public string SessionTicket { get; }
        public bool NewlyCreated { get; }

        public PlayFabAuthSession(string playFabId, string guestCustomId, string sessionTicket, bool newlyCreated)
        {
            PlayFabId = playFabId;
            GuestCustomId = guestCustomId;
            SessionTicket = sessionTicket;
            NewlyCreated = newlyCreated;
        }
    }
}
