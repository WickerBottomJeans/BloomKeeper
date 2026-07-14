using System;

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
        public string EntityId { get; }
        public string EntityType { get; }
        public string EntityToken { get; }
        public DateTime? EntityTokenExpiration { get; }
        public bool NewlyCreated { get; }

        public PlayFabAuthSession(string playFabId, string guestCustomId, string sessionTicket, string entityId, string entityType, string entityToken, DateTime? entityTokenExpiration, bool newlyCreated)
        {
            PlayFabId = playFabId;
            GuestCustomId = guestCustomId;
            SessionTicket = sessionTicket;
            EntityId = entityId;
            EntityType = entityType;
            EntityToken = entityToken;
            EntityTokenExpiration = entityTokenExpiration;
            NewlyCreated = newlyCreated;
        }
    }
}
