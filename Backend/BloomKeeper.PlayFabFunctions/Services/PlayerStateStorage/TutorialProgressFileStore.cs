using System.Text;
using DefaultNamespace;
using Newtonsoft.Json;
using PlayFab.DataModels;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

public class TutorialProgressFileStore
{
    public string FileName => "tutorial-progress.json";

    public async Task<(PlayerTutorialProgressData playerTutorialProgressData, bool fileExists)> LoadTutorialProgress(PlayFabEntityFileClient playFabEntityFileClient, GetFilesResponse entityFilesResponse)
    {
        if (!playFabEntityFileClient.TryGetFileMetadata(entityFilesResponse, FileName, out GetFileMetadata tutorialFileMetadata)) return (new PlayerTutorialProgressData(), false);
        string json = await playFabEntityFileClient.DownloadText(tutorialFileMetadata);
        PlayerTutorialProgressData playerTutorialProgressData = JsonConvert.DeserializeObject<PlayerTutorialProgressData>(json);
        TutorialProgressContract.ValidateTutorialProgress(playerTutorialProgressData);
        return (playerTutorialProgressData, true);
    }

    public byte[] SerializeTutorialProgress(PlayerTutorialProgressData playerTutorialProgressData)
    {
        TutorialProgressContract.ValidateTutorialProgress(playerTutorialProgressData);
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(playerTutorialProgressData));
    }
}
