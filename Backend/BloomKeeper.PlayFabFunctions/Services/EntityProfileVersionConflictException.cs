namespace BloomKeeper.PlayFabFunctions.Services;

public sealed class EntityProfileVersionConflictException : Exception
{
    public EntityProfileVersionConflictException(string message) : base(message)
    {
    }
}
