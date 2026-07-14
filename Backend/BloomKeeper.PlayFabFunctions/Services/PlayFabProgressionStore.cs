using BloomKeeper.PlayFabFunctions.Models;
using System.Net.Http;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.DataModels;
using PlayFab.Internal;
using DataEntityKey = PlayFab.DataModels.EntityKey;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabProgressionStore
{
    private const string ProgressionFileName = "progression.json";
    private static readonly HttpClient HttpClient = new HttpClient();

    public async Task<PlayerProgressionData> LoadProgression(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity)
    {
        GetFilesResponse getFilesResponse = await GetProgressionFiles(dataApi, dataEntity);
        if (TryGetProgressionFile(getFilesResponse, out GetFileMetadata progressionFile))
            return await DownloadProgression(progressionFile);

        PlayerProgressionData newProgression = new PlayerProgressionData();
        await UploadProgression(dataApi, dataEntity, newProgression, getFilesResponse.ProfileVersion);
        return newProgression;
    }

    private static async Task<GetFilesResponse> GetProgressionFiles(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity)
    {
        var request = new GetFilesRequest { Entity = dataEntity };
        PlayFabResult<GetFilesResponse> result = await dataApi.GetFilesAsync(request);
        return GetRequiredPlayFabResult(result, "GetFiles");
    }

    private static bool TryGetProgressionFile(GetFilesResponse response, out GetFileMetadata progressionFile)
    {
        progressionFile = null!;
        if (response.Metadata == null || !response.Metadata.TryGetValue(ProgressionFileName, out progressionFile) || progressionFile == null)
            return false;

        return true;
    }

    private static async Task<PlayerProgressionData> DownloadProgression(GetFileMetadata progressionFile)
    {
        if (string.IsNullOrWhiteSpace(progressionFile.DownloadUrl)) throw new InvalidOperationException("PlayFab progression file has no download URL.");

        string json = await HttpClient.GetStringAsync(progressionFile.DownloadUrl);
        return CreateProgressionFromJson(json);
    }

    private static PlayerProgressionData CreateProgressionFromJson(string json)
    {
        PlayerProgressionData progression = JsonConvert.DeserializeObject<PlayerProgressionData>(json);
        return ValidateProgression(progression);
    }

    private static PlayerProgressionData ValidateProgression(PlayerProgressionData progression)
    {
        if (progression == null) throw new InvalidOperationException("PlayFab progression file has invalid JSON.");
        if (progression.schemaVersion <= 0) throw new InvalidOperationException($"PlayFab progression file has invalid schema version: {progression.schemaVersion}.");
        if (progression.levels == null) throw new InvalidOperationException("PlayFab progression file has no level data map.");

        return progression;
    }

    private static async Task UploadProgression(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity, PlayerProgressionData progression, int profileVersion)
    {
        var initiateRequest = new InitiateFileUploadsRequest
        {
            Entity = dataEntity,
            ProfileVersion = profileVersion,
            FileNames = new List<string> { ProgressionFileName }
        };

        PlayFabResult<InitiateFileUploadsResponse> initiateResult = await dataApi.InitiateFileUploadsAsync(initiateRequest);
        InitiateFileUploadsResponse uploadResponse = GetRequiredPlayFabResult(initiateResult, "InitiateFileUploads");
        InitiateFileUploadMetadata uploadMetadata = GetRequiredUploadMetadata(uploadResponse);

        string json = JsonConvert.SerializeObject(progression);
        using var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(json));
        using HttpResponseMessage uploadResult = await HttpClient.PutAsync(uploadMetadata.UploadUrl, content);
        uploadResult.EnsureSuccessStatusCode();

        var finalizeRequest = new FinalizeFileUploadsRequest { Entity = dataEntity, ProfileVersion = uploadResponse.ProfileVersion, FileNames = new List<string> { ProgressionFileName } };
        PlayFabResult<FinalizeFileUploadsResponse> finalizeResult = await dataApi.FinalizeFileUploadsAsync(finalizeRequest);
        GetRequiredPlayFabResult(finalizeResult, "FinalizeFileUploads");
    }

    private static InitiateFileUploadMetadata GetRequiredUploadMetadata(InitiateFileUploadsResponse uploadResponse)
    {
        if (uploadResponse.UploadDetails == null || uploadResponse.UploadDetails.Count == 0) throw new InvalidOperationException("PlayFab did not return a progression upload URL.");

        InitiateFileUploadMetadata uploadMetadata = uploadResponse.UploadDetails[0];
        if (uploadMetadata == null || string.IsNullOrWhiteSpace(uploadMetadata.UploadUrl)) throw new InvalidOperationException("PlayFab did not return a progression upload URL.");

        return uploadMetadata;
    }

    private static T GetRequiredPlayFabResult<T>(PlayFabResult<T> result, string operationName) where T : PlayFabResultCommon
    {
        if (result == null) throw new InvalidOperationException($"PlayFab {operationName} returned no result.");
        if (result.Error != null) throw new InvalidOperationException($"PlayFab {operationName} failed: {result.Error.GenerateErrorReport()}");
        if (result.Result == null) throw new InvalidOperationException($"PlayFab {operationName} returned no response body.");

        return result.Result;
    }
}
