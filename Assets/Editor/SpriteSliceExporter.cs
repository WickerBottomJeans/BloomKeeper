using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SpriteSliceExporter
{
    private const string ExportRoot = @"C:\Src\Personal\BloomKeeperGame\AI_Images\Exported";

    [MenuItem("Assets/Export Selected Sprite Slices")]
    private static void ExportSelectedSpriteSlices()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrWhiteSpace(assetPath))
            throw new InvalidOperationException("Select a sliced PNG texture in the Project window before exporting.");
        if (!string.Equals(Path.GetExtension(assetPath), ".png", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Selected asset is not a PNG texture: {assetPath}");

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
            throw new InvalidOperationException($"Selected texture must use Sprite Mode: Multiple: {assetPath}");

        Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (sourceTexture == null)
            throw new InvalidOperationException($"Could not load the selected texture: {assetPath}");

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
        if (sprites.Length == 0)
            throw new InvalidOperationException($"Selected texture contains no sprite slices: {assetPath}");

        string textureName = Path.GetFileNameWithoutExtension(assetPath);
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            textureName = textureName.Replace(invalidCharacter, '_');
        string outputDirectory = Path.Combine(ExportRoot, textureName);
        Directory.CreateDirectory(outputDirectory);

        RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previousRenderTexture = RenderTexture.active;
        Texture2D readableTexture = null;

        try
        {
            Graphics.Blit(sourceTexture, temporaryRenderTexture);
            RenderTexture.active = temporaryRenderTexture;
            readableTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0f, 0f, sourceTexture.width, sourceTexture.height), 0, 0);
            readableTexture.Apply();

            foreach (Sprite sprite in sprites)
            {
                Rect spriteRect = sprite.rect;
                int x = Mathf.RoundToInt(spriteRect.x);
                int y = Mathf.RoundToInt(spriteRect.y);
                int width = Mathf.RoundToInt(spriteRect.width);
                int height = Mathf.RoundToInt(spriteRect.height);
                var exportedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

                try
                {
                    exportedTexture.SetPixels(readableTexture.GetPixels(x, y, width, height));
                    exportedTexture.Apply();
                    string fileName = sprite.name;
                    foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
                        fileName = fileName.Replace(invalidCharacter, '_');
                    File.WriteAllBytes(Path.Combine(outputDirectory, $"{fileName}.png"), exportedTexture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(exportedTexture);
                }
            }
        }
        finally
        {
            if (readableTexture != null)
                UnityEngine.Object.DestroyImmediate(readableTexture);
            RenderTexture.active = previousRenderTexture;
            RenderTexture.ReleaseTemporary(temporaryRenderTexture);
        }

        Debug.Log($"Exported {sprites.Length} sprite slices from {assetPath} to {outputDirectory}.");
    }
}
