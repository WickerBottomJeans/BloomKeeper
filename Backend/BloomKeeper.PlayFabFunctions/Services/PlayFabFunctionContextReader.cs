using BloomKeeper.PlayFabFunctions.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.DataModels;
using DataEntityKey = PlayFab.DataModels.EntityKey;

namespace BloomKeeper.PlayFabFunctions.Services;

public class PlayFabFunctionContextReader
{
    public async Task<PlayFabFunctionExecutionContext> ReadContext(HttpRequest request)
    {
        string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(requestBody)) throw new InvalidOperationException("PlayFab function context request body is empty.");

        PlayFabFunctionExecutionContext context = JsonConvert.DeserializeObject<PlayFabFunctionExecutionContext>(requestBody);
        if (context == null) throw new InvalidOperationException("PlayFab function context JSON is invalid.");

        return context;
    }

    public DataEntityKey GetCallerEntity(PlayFabFunctionExecutionContext context)
    {
        if (context == null) throw new InvalidOperationException("PlayFab function context is missing.");
        if (context.CallerEntityProfile == null) throw new InvalidOperationException("PlayFab caller entity profile is missing.");
        if (context.CallerEntityProfile.Entity == null) throw new InvalidOperationException("PlayFab caller entity key is missing.");
        if (string.IsNullOrWhiteSpace(context.CallerEntityProfile.Entity.Id)) throw new InvalidOperationException("PlayFab caller entity ID is missing.");
        if (string.IsNullOrWhiteSpace(context.CallerEntityProfile.Entity.Type)) throw new InvalidOperationException("PlayFab caller entity type is missing.");

        return new DataEntityKey { Id = context.CallerEntityProfile.Entity.Id, Type = context.CallerEntityProfile.Entity.Type };
    }

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
}
