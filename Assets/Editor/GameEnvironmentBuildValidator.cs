using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;

namespace DefaultNamespace.Editor
{
    public class GameEnvironmentBuildValidator : BuildPlayerProcessor
    {
        public override int callbackOrder => int.MaxValue;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            // Read the environment config from scene dependencies.
            GameEnvironmentConfig[] gameEnvironmentConfigs = AssetDatabase.GetDependencies(buildPlayerContext.BuildPlayerOptions.scenes)
                .Where(path => typeof(GameEnvironmentConfig).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
                .Select(AssetDatabase.LoadAssetAtPath<GameEnvironmentConfig>).ToArray();
            if (gameEnvironmentConfigs.Length != 1)
                throw new BuildFailedException($"Build scenes must reference exactly one GameEnvironmentConfig asset. Found {gameEnvironmentConfigs.Length}.");
            GameEnvironmentConfig gameEnvironmentConfig = gameEnvironmentConfigs[0];
            BuildTarget buildTarget = buildPlayerContext.BuildPlayerOptions.target;

            // Check the selected profile.
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings.activeProfileId != gameEnvironmentConfig.AddressablesProfileId)
                throw new BuildFailedException($"Select the Addressables profile assigned to '{gameEnvironmentConfig.name}' before building the player.");

            // Check the content actually built for the player.
            string expectedCatalogDirectory = new Uri(addressableSettings.RemoteCatalogLoadPath.GetValue(addressableSettings).TrimEnd('/') + "/").AbsoluteUri;
            string settingsPath = Path.Combine(Addressables.BuildPath, "settings.json");
            if (!File.Exists(settingsPath))
                throw new BuildFailedException("Addressables content is missing. Build Addressables for the selected environment before building the player.");
            ResourceManagerRuntimeData runtimeData = JsonUtility.FromJson<ResourceManagerRuntimeData>(File.ReadAllText(settingsPath));
            string[] remoteCatalogUrls = runtimeData.CatalogLocations.Select(location => location.InternalId).Where(url => Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)).ToArray();
            if (runtimeData.BuildTarget != buildTarget.ToString() || remoteCatalogUrls.Length == 0 || remoteCatalogUrls.Any(url => new Uri(new Uri(url), ".").AbsoluteUri != expectedCatalogDirectory))
                throw new BuildFailedException($"Built Addressables data does not match '{gameEnvironmentConfig.name}' on {buildTarget}. Rebuild Addressables with catalog destination '{expectedCatalogDirectory}' before building the player.");
        }
    }
}
