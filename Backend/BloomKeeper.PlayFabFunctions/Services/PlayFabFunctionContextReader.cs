using BloomKeeper.PlayFabFunctions.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.DataModels;
using DataEntityKey = PlayFab.DataModels.EntityKey;
using EconomyEntityKey = PlayFab.EconomyModels.EntityKey;

namespace BloomKeeper.PlayFabFunctions.Services;

/// <summary>
/// Reads PlayFab function data from Azure requests.
/// </summary>
public class PlayFabFunctionContextReader
{
    /// <summary>
    /// Reads the PlayFab function context from an HTTP request.
    /// </summary>
    public async Task<PlayFabFunctionExecutionContext> ReadContext(HttpRequest request)
    {
        string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(requestBody)) throw new InvalidOperationException("PlayFab function context request body is empty.");

        PlayFabFunctionExecutionContext context = JsonConvert.DeserializeObject<PlayFabFunctionExecutionContext>(requestBody);
        if (context == null) throw new InvalidOperationException("PlayFab function context JSON is invalid.");

        return context;
    }

    /// <summary>
    /// Gets the player entity for PlayFab Data calls.
    /// </summary>
    public DataEntityKey GetCallerEntity(PlayFabFunctionExecutionContext context)
    {
        if (context == null) throw new InvalidOperationException("PlayFab function context is missing.");
        if (context.CallerEntityProfile == null) throw new InvalidOperationException("PlayFab caller entity profile is missing.");
        if (context.CallerEntityProfile.Entity == null) throw new InvalidOperationException("PlayFab caller entity key is missing.");
        if (string.IsNullOrWhiteSpace(context.CallerEntityProfile.Entity.Id)) throw new InvalidOperationException("PlayFab caller entity ID is missing.");
        if (string.IsNullOrWhiteSpace(context.CallerEntityProfile.Entity.Type)) throw new InvalidOperationException("PlayFab caller entity type is missing.");

        return new DataEntityKey { Id = context.CallerEntityProfile.Entity.Id, Type = context.CallerEntityProfile.Entity.Type };
    }

    /// <summary>
    /// Gets the player entity for PlayFab Economy calls.
    /// </summary>
    public EconomyEntityKey GetCallerEconomyEntity(PlayFabFunctionExecutionContext context)
    {
        DataEntityKey callerEntity = GetCallerEntity(context);
        return new EconomyEntityKey { Id = callerEntity.Id, Type = callerEntity.Type };
    }

    /// <summary>
    /// Gets the function argument DTO from the function context.
    /// </summary>
    public T GetFunctionArgument<T>(PlayFabFunctionExecutionContext context)
    {
        if (context == null) throw new InvalidOperationException("PlayFab function context is missing.");
        if (context.FunctionArgument == null) throw new InvalidOperationException("PlayFab function argument is missing.");

        string json = JsonConvert.SerializeObject(context.FunctionArgument);
        T argument = JsonConvert.DeserializeObject<T>(json);
        if (argument is null) throw new InvalidOperationException("PlayFab function argument JSON is invalid.");

        return argument;
    }

    /// <summary>
    /// Creates a PlayFab Data API for this function call.
    /// </summary>
    public PlayFabDataInstanceAPI CreateDataApi(PlayFabFunctionExecutionContext context)
    {
        if (context == null) throw new InvalidOperationException("PlayFab function context is missing.");
        if (context.TitleAuthenticationContext == null) throw new InvalidOperationException("PlayFab title authentication context is missing.");
        if (string.IsNullOrWhiteSpace(context.TitleAuthenticationContext.Id)) throw new InvalidOperationException("PlayFab title ID is missing.");
        if (string.IsNullOrWhiteSpace(context.TitleAuthenticationContext.EntityToken)) throw new InvalidOperationException("PlayFab title entity token is missing.");

        var apiSettings = new PlayFabApiSettings { TitleId = context.TitleAuthenticationContext.Id };
        var authContext = new PlayFabAuthenticationContext { EntityToken = context.TitleAuthenticationContext.EntityToken };
        return new PlayFabDataInstanceAPI(apiSettings, authContext);
    }

    /// <summary>
    /// Creates a PlayFab Economy API for this function call.
    /// </summary>
    public PlayFabEconomyInstanceAPI CreateEconomyApi(PlayFabFunctionExecutionContext context)
    {
        if (context == null) throw new InvalidOperationException("PlayFab function context is missing.");
        if (context.TitleAuthenticationContext == null) throw new InvalidOperationException("PlayFab title authentication context is missing.");
        if (string.IsNullOrWhiteSpace(context.TitleAuthenticationContext.Id)) throw new InvalidOperationException("PlayFab title ID is missing.");
        if (string.IsNullOrWhiteSpace(context.TitleAuthenticationContext.EntityToken)) throw new InvalidOperationException("PlayFab title entity token is missing.");

        var apiSettings = new PlayFabApiSettings { TitleId = context.TitleAuthenticationContext.Id };
        var authContext = new PlayFabAuthenticationContext { EntityToken = context.TitleAuthenticationContext.EntityToken };
        return new PlayFabEconomyInstanceAPI(apiSettings, authContext);
    }
}
