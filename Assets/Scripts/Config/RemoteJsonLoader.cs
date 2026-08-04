using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace DefaultNamespace
{
    public class RemoteJsonLoader
    {
        private readonly Uri baseUri;

        public RemoteJsonLoader(string baseUrl)
        {
            baseUri = new Uri(baseUrl, UriKind.Absolute);
        }

        public async UniTask<T> LoadAsync<T>(string relativePath)
        {
            Uri uri = new Uri(baseUri, relativePath);
            using (UnityWebRequest request = UnityWebRequest.Get(uri.AbsoluteUri))
            {
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new IOException($"Unable to load remote config '{uri.AbsoluteUri}': {request.error}");

                try
                {
                    return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                }
                catch (JsonException exception)
                {
                    throw new InvalidDataException($"Remote config '{uri.AbsoluteUri}' contains invalid JSON.", exception);
                }
            }
        }
    }
}
