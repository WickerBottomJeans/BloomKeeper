namespace BloomKeeper.PlayFabFunctions.Models;

public class PlayerLivesConfig
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion;
    public int maximumLives;
    public int regenerationIntervalSeconds;
}
