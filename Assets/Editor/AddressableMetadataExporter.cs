#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace DefaultNamespace.Editor
{
    public class AddressableMetadataExporter : EditorWindow
    {
        private string groupName = "";
        private string outputFolder = "Assets/StreamingAssets/";
        private string fileName = "";

        [MenuItem("Tools/Addressable Metadata Exporter")]
        public static void ShowWindow()
        {
            GetWindow<AddressableMetadataExporter>("Addressable Metadata Exporter");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Addressable Metadata Exporter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            groupName = EditorGUILayout.TextField("Group Name", groupName);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            fileName = EditorGUILayout.TextField("File Name", fileName);
            EditorGUILayout.Space();

            if (GUILayout.Button("Export"))
                Export();
        }

        private void Export()
        {
            if (string.IsNullOrEmpty(groupName)) { Debug.LogError("Group name is empty."); return; }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(groupName);

            if (group == null) { Debug.LogError($"Group '{groupName}' not found."); return; }

            var entries = new List<AssetMetadata>();

            foreach (var entry in group.entries)
            {
                var metadata = new AssetMetadata { address = entry.address };
                var parts = entry.address.Split('_');
                if (parts.Length > 0 && int.TryParse(parts.Last(), out int parsedIndex))
                {
                    metadata.index = parsedIndex;
                }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.AssetPath);
                if (texture != null)
                {
                    metadata.width = texture.width;
                    metadata.height = texture.height;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.AssetPath);
                if (sprite != null)
                {
                    metadata.width = (int)sprite.rect.width;
                    metadata.height = (int)sprite.rect.height;
                }

                entries.Add(metadata);
                Debug.Log($"Exported {entry.address}: {metadata.width}x{metadata.height}");
            }

            var manifest = new AssetManifest { assets = entries };
            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);

            string outputPath = Path.Combine(outputFolder, $"{fileName}.json");
            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(outputPath, json);
            AssetDatabase.Refresh();
            Debug.Log($"Manifest exported to {outputPath}");
        }
    }
}
#endif