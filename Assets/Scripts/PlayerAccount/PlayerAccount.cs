namespace DefaultNamespace
{
    public class PlayerAccount
    {
        public PlayFabAuthSession AuthSession { get; }
        public PlayerProgressionData Progression { get; }

        public PlayerAccount(PlayFabAuthSession authSession, PlayerProgressionData progression)
        {
            AuthSession = authSession;
            Progression = progression;
        }
    }
}
