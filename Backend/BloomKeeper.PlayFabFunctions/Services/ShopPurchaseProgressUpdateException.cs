namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// Signals that purchase progress could not be safely recorded.
/// </summary>
public class ShopPurchaseProgressUpdateException : Exception
{
    public ShopPurchaseProgressUpdateException(string message, Exception innerException = null) : base(message, innerException)
    {
    }
}
