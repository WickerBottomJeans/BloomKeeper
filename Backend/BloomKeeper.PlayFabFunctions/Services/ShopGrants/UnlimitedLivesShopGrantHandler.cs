using BloomKeeper.PlayFabFunctions.Models;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services.ShopGrants;

/// <summary>
/// Adds unlimited lives time from one shop grant.
/// </summary>
public class UnlimitedLivesShopGrantHandler : IShopGrantHandler
{
    private const int MaxWriteAttempts = 3;
    private const int InitialConflictRetryDelayMilliseconds = 100;

    private readonly PlayFabFunctionContextReader contextReader;
    private readonly PlayFabLivesConfigService livesConfigService;
    private readonly PlayFabEntityFileClient fileClient;
    private readonly LivesFileStore livesStore;
    private readonly LivesService livesService;

    public ShopGrantKind GrantKind => ShopGrantKind.UnlimitedLives;

    /// <summary>
    /// Creates the handler with its lives-file dependencies.
    /// </summary>
    public UnlimitedLivesShopGrantHandler(PlayFabFunctionContextReader contextReader, PlayFabLivesConfigService livesConfigService, PlayFabEntityFileClient fileClient, LivesFileStore livesStore, LivesService livesService)
    {
        this.contextReader = contextReader ?? throw new ArgumentNullException(nameof(contextReader));
        this.livesConfigService = livesConfigService ?? throw new ArgumentNullException(nameof(livesConfigService));
        this.fileClient = fileClient ?? throw new ArgumentNullException(nameof(fileClient));
        this.livesStore = livesStore ?? throw new ArgumentNullException(nameof(livesStore));
        this.livesService = livesService ?? throw new ArgumentNullException(nameof(livesService));
    }

    /// <summary>
    /// Adds the configured unlimited lives duration to the player's lives file.
    /// </summary>
    public async Task ApplyShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext context, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken)
    {
        if (shopGrantConfig == null) throw new ArgumentNullException(nameof(shopGrantConfig));
        if (shopGrantConfig.unlimitedLives == null) throw new InvalidOperationException($"Shop grant {shopGrantConfig.grantId} has no unlimited lives payload.");
        if (string.IsNullOrWhiteSpace(shopGrantIdempotencyKey)) throw new ArgumentException("Shop grant idempotency key is missing.", nameof(shopGrantIdempotencyKey));

        var dataApi = contextReader.CreateDataApi(context);
        var dataEntity = contextReader.GetCallerEntity(context);
        PlayerLivesConfig livesConfig = await livesConfigService.Load(context.TitleAuthenticationContext.Id);

        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileMetadata = await fileClient.LoadEntityFileMetadata(dataApi, dataEntity);
            (PlayerLivesData lives, _) = await livesStore.Load(fileClient, fileMetadata, livesConfig.maximumLives);
            livesService.GrantUnlimitedLives(lives, livesConfig, operationTimeUtc, shopGrantConfig.unlimitedLives.durationSeconds);

            try
            {
                await fileClient.UploadFile(dataApi, dataEntity, livesStore.FileName, livesStore.Serialize(lives, livesConfig.maximumLives), fileMetadata.ProfileVersion);
                return;
            }
            catch (EntityProfileVersionConflictException) when (writeAttempt < MaxWriteAttempts)
            {
                int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unlimited lives grant exhausted its write attempts without completing.");
    }

    /// <summary>
    /// Removes the configured unlimited lives duration from the player's lives file.
    /// </summary>
    public async Task RevertShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext playFabFunctionExecutionContext, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken)
    {
        if (shopGrantConfig == null) throw new ArgumentNullException(nameof(shopGrantConfig));
        if (shopGrantConfig.unlimitedLives == null) throw new InvalidOperationException($"Shop grant {shopGrantConfig.grantId} has no unlimited lives payload.");
        if (string.IsNullOrWhiteSpace(shopGrantIdempotencyKey)) throw new ArgumentException("Shop grant idempotency key is missing.", nameof(shopGrantIdempotencyKey));

        var playFabDataApi = contextReader.CreateDataApi(playFabFunctionExecutionContext);
        var callerDataEntity = contextReader.GetCallerEntity(playFabFunctionExecutionContext);
        PlayerLivesConfig playerLivesConfig = await livesConfigService.Load(playFabFunctionExecutionContext.TitleAuthenticationContext.Id);

        for (int writeAttempt = 1; writeAttempt <= MaxWriteAttempts; writeAttempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entityFilesResponse = await fileClient.LoadEntityFileMetadata(playFabDataApi, callerDataEntity);
            (PlayerLivesData playerLivesData, _) = await livesStore.Load(fileClient, entityFilesResponse, playerLivesConfig.maximumLives);
            livesService.SubtractUnlimitedLivesDuration(playerLivesData, operationTimeUtc, shopGrantConfig.unlimitedLives.durationSeconds);

            try
            {
                await fileClient.UploadFile(playFabDataApi, callerDataEntity, livesStore.FileName, livesStore.Serialize(playerLivesData, playerLivesConfig.maximumLives), entityFilesResponse.ProfileVersion);
                return;
            }
            catch (EntityProfileVersionConflictException) when (writeAttempt < MaxWriteAttempts)
            {
                int delayMilliseconds = InitialConflictRetryDelayMilliseconds * (1 << (writeAttempt - 1));
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }

        throw new InvalidOperationException("Unlimited lives grant reversion exhausted its write attempts without completing.");
    }
}
