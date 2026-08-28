using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// Adds and subtracts PlayFab currency balances.
/// </summary>
public class PlayFabCurrencyService
{
    private readonly PlayFabFunctionContextReader playFabFunctionContextReader = new PlayFabFunctionContextReader();
    private readonly PlayFabInventoryService playFabInventoryService = new PlayFabInventoryService();

    /// <summary>
    /// Adds an amount to the player's currency balance.
    /// </summary>
    public async Task AddCurrency(PlayFabFunctionExecutionContext playFabFunctionExecutionContext, CurrencyKind currencyKind, int currencyAmount, string currencyMutationIdempotencyKey)
    {
        var playFabEconomyApi = playFabFunctionContextReader.CreateEconomyApi(playFabFunctionExecutionContext);
        var callerEconomyEntity = playFabFunctionContextReader.GetCallerEconomyEntity(playFabFunctionExecutionContext);
        await playFabInventoryService.AddInventoryItem(playFabEconomyApi, callerEconomyEntity, GetCurrencyCatalogId(currencyKind), currencyAmount, currencyMutationIdempotencyKey);
    }

    /// <summary>
    /// [Duong] Subtracts an amount from the player's currency balance, or returns false when there is not enough.
    /// </summary>
    public async Task<bool> TrySubtractCurrency(PlayFabFunctionExecutionContext playFabFunctionExecutionContext, CurrencyKind currencyKind, int currencyAmount, string currencyMutationIdempotencyKey)
    {
        var playFabEconomyApi = playFabFunctionContextReader.CreateEconomyApi(playFabFunctionExecutionContext);
        var callerEconomyEntity = playFabFunctionContextReader.GetCallerEconomyEntity(playFabFunctionExecutionContext);
        return await playFabInventoryService.TrySubtractInventoryItem(playFabEconomyApi, callerEconomyEntity, GetCurrencyCatalogId(currencyKind), currencyAmount, currencyMutationIdempotencyKey);
    }

    private static string GetCurrencyCatalogId(CurrencyKind currencyKind)
    {
        return currencyKind switch
        {
            CurrencyKind.Diamonds => PlayerInventoryCatalogIds.DiamondsCatalogId,
            _ => throw new ArgumentOutOfRangeException(nameof(currencyKind), currencyKind, "Unsupported currency kind.")
        };
    }
}
