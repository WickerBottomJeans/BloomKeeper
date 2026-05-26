using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

public class TexturePackerImporter : EditorWindow
{
    [MenuItem("Tools/Import TexturePacker JSON")]
    public static void ShowWindow()
    {
        GetWindow<TexturePackerImporter>("TP Importer");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Import Atlas"))
            Import();
    }

    private static void Import()
    {
        string jsonPath = EditorUtility.OpenFilePanel("Select JSON Hash file", "Assets", "json");
        if (string.IsNullOrEmpty(jsonPath)) return;

        string json = File.ReadAllText(jsonPath);
        var data = JsonConvert.DeserializeObject<TPAtlas>(json);

        // Expect PNG next to JSON with same name
        string pngPath = Path.ChangeExtension(jsonPath, ".png");
        string assetPath = "Assets" + pngPath.Substring(Application.dataPath.Length);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not find texture at {assetPath}. Make sure PNG is inside Assets.");
            return;
        }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Bilinear;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;

        var metas = new List<SpriteMetaData>();
        int textureHeight = data.meta.size.h;

        foreach (var kvp in data.frames)
        {
            var frame = kvp.Value.frame;
            var meta  = new SpriteMetaData
            {
                name   = Path.GetFileNameWithoutExtension(kvp.Key),
                rect   = new Rect(frame.x, textureHeight - frame.y - frame.h, frame.w, frame.h),
                pivot  = new Vector2(0.5f, 0.5f),
                alignment = (int)SpriteAlignment.Center
            };
            metas.Add(meta);
        }

        importer.spritesheet = metas.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"Imported {metas.Count} sprites from {Path.GetFileName(jsonPath)}");
    }

    // JSON deserialization models
    private class TPAtlas
    {
        public Dictionary<string, TPFrame> frames;
        public TPMeta meta;
    }

    private class TPFrame
    {
        public TPRect frame;
    }

    private class TPRect
    {
        public int x, y, w, h;
    }

    private class TPMeta
    {
        public TPSize size;
    }

    private class TPSize
    {
        public int w, h;
    }
}