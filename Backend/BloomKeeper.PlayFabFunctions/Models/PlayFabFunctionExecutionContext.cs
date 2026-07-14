using PlayFab.ProfilesModels;

namespace BloomKeeper.PlayFabFunctions.Models;

public class PlayFabFunctionExecutionContext
{
    public EntityProfileBody CallerEntityProfile { get; set; } = null!;
    public TitleAuthenticationContext TitleAuthenticationContext { get; set; } = null!;
    public object FunctionArgument { get; set; } = null!;
}

public class TitleAuthenticationContext
{
    public string Id { get; set; } = null!;
    public string EntityToken { get; set; } = null!;
}
