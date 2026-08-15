namespace DefaultNamespace
{
    public static class LoadPlayerStateContract
    {
        public const int CurrentSchemaVersion = 1;
    }

    public class LoadPlayerStateResponse
    {
        public int schemaVersion = LoadPlayerStateContract.CurrentSchemaVersion;
        public PlayerProgressionData progression;
        public PlayerLivesSnapshot lives;
    }
}
