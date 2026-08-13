using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DefaultNamespace.Editor
{
    [InitializeOnLoad]
    public static class AddressablesCatalogUrlWorkaround
    {
        private const string BrokenCatalogSeparator = "//catalog_";
        private const string CorrectCatalogSeparator = "/catalog_";

        static AddressablesCatalogUrlWorkaround()
        {
            BuildScript.buildCompleted -= HandleAddressablesBuildCompleted;
            BuildScript.buildCompleted += HandleAddressablesBuildCompleted;
        }
    
        private static void HandleAddressablesBuildCompleted(AddressableAssetBuildResult result)
        {
            if (result is not AddressablesPlayerBuildResult || !string.IsNullOrEmpty(result.Error))
                return;

            RepairGeneratedSettings(result.OutputPath);
        }

        public static void RepairGeneratedSettings(string settingsPath)
        {
            // Replace only Unity's malformed catalog separator
            string json = File.ReadAllText(settingsPath);
            string repairedJson = json.Replace(BrokenCatalogSeparator, CorrectCatalogSeparator);
            if (repairedJson == json)
                return;

            File.WriteAllText(settingsPath, repairedJson);
            Debug.Log($"Repaired malformed Addressables catalog URLs in '{settingsPath}'.");
        }

        public static void ValidateGeneratedSettings(string settingsPath)
        {
            if (!File.Exists(settingsPath))
                throw new BuildFailedException($"Addressables runtime settings do not exist at '{settingsPath}'. Build Addressables content before building the player.");

            if (File.ReadAllText(settingsPath).Contains(BrokenCatalogSeparator))
                throw new BuildFailedException($"Addressables runtime settings at '{settingsPath}' still contain a malformed '//catalog_' URL. Rebuild Addressables content before building the player.");
        }
    }

    public class AddressablesCatalogUrlBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || !settings.BuildRemoteCatalog)
                return;

            AddressablesCatalogUrlWorkaround.ValidateGeneratedSettings(Path.Combine(Addressables.BuildPath, "settings.json"));
        }
    }
}
