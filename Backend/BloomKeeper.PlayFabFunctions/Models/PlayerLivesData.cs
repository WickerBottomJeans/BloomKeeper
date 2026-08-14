namespace BloomKeeper.PlayFabFunctions.Models;

public class PlayerLivesData
{
    public int schemaVersion = 1;
    public int availableLives;
    public DateTimeOffset? regenerationAnchorUtc;
}
