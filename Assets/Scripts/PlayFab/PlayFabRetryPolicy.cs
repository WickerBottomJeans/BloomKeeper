using PlayFab;
using PlayFab.CloudScriptModels;

namespace DefaultNamespace
{
    public static class PlayFabRetryPolicy
    {
        // https://learn.microsoft.com/en-us/xbox/playfab/api-references/http-response-status-codes
        // https://learn.microsoft.com/en-us/xbox/playfab/api-references/global-api-method-error-codes
        public static bool IsRetryable(PlayFabError error)
        {
            if (error == null) return false;
            if (error.HttpCode == 408 || error.HttpCode == 409 || error.HttpCode == 429 || error.HttpCode == 500 || error.HttpCode == 502 || error.HttpCode == 503 || error.HttpCode == 504) return true;
            return error.Error == PlayFabErrorCode.ServiceUnavailable || error.Error == PlayFabErrorCode.DownstreamServiceUnavailable || error.Error == PlayFabErrorCode.APIClientRequestRateLimitExceeded || error.Error == PlayFabErrorCode.APIConcurrentRequestLimitExceeded || error.Error == PlayFabErrorCode.ConcurrentEditError || error.Error == PlayFabErrorCode.DataUpdateRateExceeded;
        }

        public static bool IsRetryable(FunctionExecutionError error)
        {
            if (error == null) return false;
            return error.Error == nameof(PlayFabErrorCode.CloudScriptAzureFunctionsExecutionTimeLimitExceeded) || error.Error == nameof(PlayFabErrorCode.CloudScriptAzureFunctionsHTTPRequestError);
        }
    }
}
