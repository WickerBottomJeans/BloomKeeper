using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

public class LoadPlayerStateResponse
{
    public int schemaVersion = PlayerLivesContract.CurrentSchemaVersion;
    public PlayerProgressionData progression;
    public PlayerLivesSnapshot lives;
}
