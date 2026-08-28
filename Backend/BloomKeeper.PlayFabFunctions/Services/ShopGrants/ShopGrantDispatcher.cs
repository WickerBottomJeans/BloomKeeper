using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services.ShopGrants;

/// <summary>
/// Routes shop grants to their owning handlers.
/// </summary>
public class ShopGrantDispatcher
{
    private readonly IReadOnlyDictionary<ShopGrantKind, IShopGrantHandler> handlersByGrantKind;

    /// <summary>
    /// Registers one handler for each supported grant kind.
    /// </summary>
    public ShopGrantDispatcher(IEnumerable<IShopGrantHandler> handlers)
    {
        if (handlers == null) throw new ArgumentNullException(nameof(handlers));

        var registeredHandlers = new Dictionary<ShopGrantKind, IShopGrantHandler>();
        foreach (IShopGrantHandler handler in handlers)
        {
            if (handler == null) throw new InvalidOperationException("Shop grant handler collection contains a null handler.");
            if (!Enum.IsDefined(typeof(ShopGrantKind), handler.GrantKind)) throw new InvalidOperationException($"Shop grant handler has unsupported kind {handler.GrantKind}.");
            if (!registeredHandlers.TryAdd(handler.GrantKind, handler)) throw new InvalidOperationException($"Multiple shop grant handlers are registered for {handler.GrantKind}.");
        }

        handlersByGrantKind = registeredHandlers;
    }

    /// <summary>
    /// [Duong] Applies one shop grant through its owning handler.
    /// </summary>
    public async Task ApplyShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext context, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken)
    {
        //[Duong] Safety checks
        if (shopGrantConfig == null) throw new ArgumentNullException(nameof(shopGrantConfig));
        if (string.IsNullOrWhiteSpace(shopGrantIdempotencyKey)) throw new ArgumentException("Shop grant idempotency key is missing.", nameof(shopGrantIdempotencyKey));

        cancellationToken.ThrowIfCancellationRequested();
        if (!handlersByGrantKind.TryGetValue(shopGrantConfig.kind, out IShopGrantHandler shopGrantHandler)) throw new InvalidOperationException($"No shop grant handler is registered for {shopGrantConfig.kind}.");
        await shopGrantHandler.ApplyShopGrant(shopGrantConfig, context, shopGrantIdempotencyKey, operationTimeUtc, cancellationToken);
    }

    /// <summary>
    /// [Duong] Reverts one shop grant through its owning handler.
    /// </summary>
    public async Task RevertShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext playFabFunctionExecutionContext, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken)
    {
        if (shopGrantConfig == null) throw new ArgumentNullException(nameof(shopGrantConfig));
        if (string.IsNullOrWhiteSpace(shopGrantIdempotencyKey)) throw new ArgumentException("Shop grant idempotency key is missing.", nameof(shopGrantIdempotencyKey));

        cancellationToken.ThrowIfCancellationRequested();
        if (!handlersByGrantKind.TryGetValue(shopGrantConfig.kind, out IShopGrantHandler shopGrantHandler)) throw new InvalidOperationException($"No shop grant handler is registered for {shopGrantConfig.kind}.");
        await shopGrantHandler.RevertShopGrant(shopGrantConfig, playFabFunctionExecutionContext, shopGrantIdempotencyKey, operationTimeUtc, cancellationToken);
    }
}
