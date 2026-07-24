using PlayFab;
using PlayFab.DataModels;
using PlayFab.Internal;
using DataEntityKey = PlayFab.DataModels.EntityKey;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabEntityFileClient
{
    private static readonly HttpClient HttpClient = new HttpClient();

    public async Task<GetFilesResponse> LoadEntityFileMetadata(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity)
    {
        var request = new GetFilesRequest { Entity = dataEntity };
        PlayFabResult<GetFilesResponse> result = await dataApi.GetFilesAsync(request);
        return GetRequiredPlayFabResult(result, "GetFiles");
    }

    public bool TryGetFileMetadata(GetFilesResponse response, string fileName, out GetFileMetadata fileMetadata)
    {
        fileMetadata = null!;
        if (response.Metadata == null || !response.Metadata.TryGetValue(fileName, out fileMetadata) || fileMetadata == null) return false;

        return true;
    }

    public async Task<string> DownloadText(GetFileMetadata fileMetadata)
    {
        if (fileMetadata == null || string.IsNullOrWhiteSpace(fileMetadata.DownloadUrl)) throw new InvalidOperationException("PlayFab file has no download URL.");

        return await HttpClient.GetStringAsync(fileMetadata.DownloadUrl);
    }

    public async Task UploadFile(PlayFabDataInstanceAPI dataApi, DataEntityKey dataEntity, string fileName, byte[] bytes, int profileVersion)
    {
        var initiateRequest = new InitiateFileUploadsRequest { Entity = dataEntity, ProfileVersion = profileVersion, FileNames = new List<string> { fileName } };
        PlayFabResult<InitiateFileUploadsResponse> initiateResult = await dataApi.InitiateFileUploadsAsync(initiateRequest);
        if (initiateResult?.Error != null && (initiateResult.Error.Error == PlayFabErrorCode.EntityProfileVersionMismatch || initiateResult.Error.Error == PlayFabErrorCode.ConcurrentEditError))
            throw new EntityProfileVersionConflictException($"PlayFab InitiateFileUploads detected an entity profile version conflict: {initiateResult.Error.GenerateErrorReport()}");
        InitiateFileUploadsResponse uploadResponse = GetRequiredPlayFabResult(initiateResult, "InitiateFileUploads");
        InitiateFileUploadMetadata uploadMetadata = GetRequiredUploadMetadata(uploadResponse);

        using var content = new ByteArrayContent(bytes);
        using HttpResponseMessage uploadResult = await HttpClient.PutAsync(uploadMetadata.UploadUrl, content);
        uploadResult.EnsureSuccessStatusCode();

        var finalizeRequest = new FinalizeFileUploadsRequest { Entity = dataEntity, ProfileVersion = uploadResponse.ProfileVersion, FileNames = new List<string> { fileName } };
        PlayFabResult<FinalizeFileUploadsResponse> finalizeResult = await dataApi.FinalizeFileUploadsAsync(finalizeRequest);
        GetRequiredPlayFabResult(finalizeResult, "FinalizeFileUploads");
    }

    private static InitiateFileUploadMetadata GetRequiredUploadMetadata(InitiateFileUploadsResponse uploadResponse)
    {
        if (uploadResponse.UploadDetails == null || uploadResponse.UploadDetails.Count == 0) throw new InvalidOperationException("PlayFab did not return a file upload URL.");

        InitiateFileUploadMetadata uploadMetadata = uploadResponse.UploadDetails[0];
        if (uploadMetadata == null || string.IsNullOrWhiteSpace(uploadMetadata.UploadUrl)) throw new InvalidOperationException("PlayFab did not return a file upload URL.");

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
