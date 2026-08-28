using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services.ShopGrants;

/// <summary>
/// Applies one kind of shop grant.
/// </summary>
public interface IShopGrantHandler
{
    ShopGrantKind GrantKind { get; }

    Task ApplyShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext context, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken);

    Task RevertShopGrant(ShopGrantConfig shopGrantConfig, PlayFabFunctionExecutionContext playFabFunctionExecutionContext, string shopGrantIdempotencyKey, DateTimeOffset operationTimeUtc, CancellationToken cancellationToken);
}
