using System;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Remote config URL and Addressables profile for one game environment.
    /// </summary>
    public class GameEnvironmentConfig : ScriptableObject
    {
        [SerializeField] private string contentRootUrl;
        [SerializeField] private string addressablesProfileId;

        public string ConfigBaseUrl => $"{contentRootUrl.TrimEnd('/')}/configs/";
        public string AddressablesProfileId => addressablesProfileId;

        /// <summary>
        /// Rejects an invalid public content URL.
        /// </summary>
        public void ValidateEnvironmentConfig()
        {
            if (!Uri.TryCreate(contentRootUrl, UriKind.Absolute, out Uri contentRootUri) || contentRootUri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(contentRootUri.Host) || !string.IsNullOrEmpty(contentRootUri.UserInfo) || !string.IsNullOrEmpty(contentRootUri.Query) || !string.IsNullOrEmpty(contentRootUri.Fragment) || contentRootUrl != contentRootUrl.Trim())
                throw new InvalidOperationException($"Environment config '{name}' needs an absolute HTTPS content root URL without credentials, a query, or a fragment.");
        }
    }
}
