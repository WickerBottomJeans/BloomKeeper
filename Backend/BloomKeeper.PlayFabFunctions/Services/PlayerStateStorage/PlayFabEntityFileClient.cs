using PlayFab;
using PlayFab.DataModels;
using PlayFab.Internal;
using BloomKeeper.PlayFabFunctions.Services;

namespace BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;

/// <summary>
/// [Duong] Generic PlayFab Entity Files wrapper: lists files and transfers their raw contents.
/// </summary>
public class PlayFabEntityFileClient
{
    private static readonly HttpClient HttpClient = new HttpClient();

    #region Public API

    /// <summary>
    /// [Duong] Just asks PlayFab for this entity's file metadata.
    /// </summary>
    public async Task<GetFilesResponse> LoadEntityFileMetadata(PlayFabDataInstanceAPI dataApi, EntityKey dataEntity)
    {
        var request = new GetFilesRequest { Entity = dataEntity };
        PlayFabResult<GetFilesResponse> result = await dataApi.GetFilesAsync(request);
        return GetRequiredPlayFabResult(result, "GetFiles");
    }

    /// <summary>
    /// [Duong] Tries to get a specific file's metadata from the entity's metadata.
    /// </summary>
    public bool TryGetFileMetadata(GetFilesResponse response, string fileName, out GetFileMetadata fileMetadata)
    {
        fileMetadata = null!;
        if (response.Metadata == null || !response.Metadata.TryGetValue(fileName, out fileMetadata) ||
            fileMetadata == null) return false;

        return true;
    }

    /// <summary>
    /// [Duong] Downloads this file from its URL as text.
    /// </summary>
    public async Task<string> DownloadText(GetFileMetadata fileMetadata)
    {
        if (fileMetadata == null || string.IsNullOrWhiteSpace(fileMetadata.DownloadUrl))
            throw new InvalidOperationException("PlayFab file has no download URL.");

        return await HttpClient.GetStringAsync(fileMetadata.DownloadUrl);
    }

    /// <summary>
    /// [Duong] Uploads files to PlayFab storage at the expected profile version.
    /// </summary>
    public async Task UploadFile(PlayFabDataInstanceAPI dataApi, EntityKey dataEntity, string fileName, byte[] bytes,
        int profileVersion)
    {
        await UploadFiles(dataApi, dataEntity, new Dictionary<string, byte[]> { { fileName, bytes } }, profileVersion);
    }

    /// <summary>
    /// [Duong] Uploads files to PlayFab storage at the expected profile version.
    /// </summary>
    public async Task UploadFiles(PlayFabDataInstanceAPI dataApi, EntityKey dataEntity,
        IReadOnlyDictionary<string, byte[]> files, int profileVersion)
    {
        if (files == null) throw new ArgumentNullException(nameof(files));
        if (files.Count == 0)
            throw new ArgumentException("At least one PlayFab Entity File is required.", nameof(files));

        // Ask PlayFab for temporary upload URLs.
        List<string> fileNames = files.Keys.ToList();
        var initiateRequest = new InitiateFileUploadsRequest
            { Entity = dataEntity, ProfileVersion = profileVersion, FileNames = fileNames };
        PlayFabResult<InitiateFileUploadsResponse> initiateResult = await dataApi.InitiateFileUploadsAsync(initiateRequest);
        ThrowIfProfileVersionConflict(initiateResult, "InitiateFileUploads");
        InitiateFileUploadsResponse uploadResponse = GetRequiredPlayFabResult(initiateResult, "InitiateFileUploads");

        Dictionary<string, InitiateFileUploadMetadata> uploadMetadataByFileName = GetRequiredUploadMetadataByFileName(uploadResponse, fileNames);

        // Upload each file's bytes to its temporary URL.
        foreach ((string fileName, byte[] bytes) in files)
        {
            using var content = new ByteArrayContent(bytes);
            using HttpResponseMessage uploadResult =
                await HttpClient.PutAsync(uploadMetadataByFileName[fileName].UploadUrl, content);
            uploadResult.EnsureSuccessStatusCode();
        }

        // Finalize the uploads in PlayFab.
        var finalizeRequest = new FinalizeFileUploadsRequest
            { Entity = dataEntity, ProfileVersion = uploadResponse.ProfileVersion, FileNames = fileNames };
        PlayFabResult<FinalizeFileUploadsResponse> finalizeResult = await dataApi.FinalizeFileUploadsAsync(finalizeRequest);
        ThrowIfProfileVersionConflict(finalizeResult, "FinalizeFileUploads");
        GetRequiredPlayFabResult(finalizeResult, "FinalizeFileUploads");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// [Duong] Gets the upload metadata for every requested file
    /// </summary>
    private Dictionary<string, InitiateFileUploadMetadata> GetRequiredUploadMetadataByFileName(
        InitiateFileUploadsResponse uploadResponse, IReadOnlyCollection<string> requestedFileNames)
    {
        if (uploadResponse.UploadDetails == null || uploadResponse.UploadDetails.Count != requestedFileNames.Count)
            throw new InvalidOperationException("PlayFab did not return every requested file upload URL.");

        var uploadMetadataByFileName = new Dictionary<string, InitiateFileUploadMetadata>();
        foreach (InitiateFileUploadMetadata uploadMetadata in uploadResponse.UploadDetails)
        {
            if (uploadMetadata == null || string.IsNullOrWhiteSpace(uploadMetadata.FileName) || string.IsNullOrWhiteSpace(uploadMetadata.UploadUrl))
                throw new InvalidOperationException("PlayFab returned invalid file upload metadata.");
            
            if (!uploadMetadataByFileName.TryAdd(uploadMetadata.FileName, uploadMetadata))
                throw new InvalidOperationException(
                    $"PlayFab returned duplicate upload metadata for '{uploadMetadata.FileName}'.");
        }

        foreach (string requestedFileName in requestedFileNames)
            if (!uploadMetadataByFileName.ContainsKey(requestedFileName))
                throw new InvalidOperationException($"PlayFab did not return an upload URL for '{requestedFileName}'.");

        return uploadMetadataByFileName;
    }

    private void ThrowIfProfileVersionConflict<T>(PlayFabResult<T> result, string operationName)
        where T : PlayFabResultCommon
    {
        if (result?.Error != null && (result.Error.Error == PlayFabErrorCode.EntityProfileVersionMismatch ||
                                      result.Error.Error == PlayFabErrorCode.ConcurrentEditError))
            throw new EntityProfileVersionConflictException(
                $"PlayFab {operationName} detected an entity profile version conflict: {result.Error.GenerateErrorReport()}");
    }

    private T GetRequiredPlayFabResult<T>(PlayFabResult<T> result, string operationName) where T : PlayFabResultCommon
    {
        if (result == null) throw new InvalidOperationException($"PlayFab {operationName} returned no result.");
        if (result.Error != null)
            throw new InvalidOperationException(
                $"PlayFab {operationName} failed: {result.Error.GenerateErrorReport()}");
        if (result.Result == null)
            throw new InvalidOperationException($"PlayFab {operationName} returned no response body.");

        return result.Result;
    }

    #endregion
}
