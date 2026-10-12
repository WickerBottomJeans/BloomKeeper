using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class SpriteRenamer
{
    private static readonly string[] TileSpriteNames =
    {
        "Tile_Normal",
        "Tile_Web",
        "Tile_Inactive"
    };

    [MenuItem("Assets/Rename Tile Sprite Slices", false, 2001)]
    private static void RenameSelectedTileSpriteSheet()
    {
        Texture2D spriteSheet = (Texture2D)Selection.activeObject;
        string path = AssetDatabase.GetAssetPath(spriteSheet);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            throw new InvalidOperationException($"Selected texture does not have a TextureImporter: {path}");

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple)
            throw new InvalidOperationException($"Selected texture must use Sprite (2D and UI) type and Multiple sprite mode: {path}");

        var dataProviderFactories = new SpriteDataProviderFactories();
        dataProviderFactories.Init();
        ISpriteEditorDataProvider dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] spriteRects = dataProvider.GetSpriteRects()
            .OrderBy(spriteRect => spriteRect.rect.center.x)
            .ToArray();

        if (spriteRects.Length != TileSpriteNames.Length)
            throw new InvalidOperationException($"Selected texture must contain exactly {TileSpriteNames.Length} slices, but contains {spriteRects.Length}: {path}");

        for (int index = 0; index < TileSpriteNames.Length; index++)
            spriteRects[index].name = TileSpriteNames[index];

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
        importer.SaveAndReimport();
    }

    [MenuItem("Assets/Set Tile Sprite Pivots", false, 2002)]
    private static void SetSelectedTileSpritePivots()
    {
        Texture2D spriteSheet = (Texture2D)Selection.activeObject;
        string path = AssetDatabase.GetAssetPath(spriteSheet);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            throw new InvalidOperationException($"Selected texture does not have a TextureImporter: {path}");

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple)
            throw new InvalidOperationException($"Selected texture must use Sprite (2D and UI) type and Multiple sprite mode: {path}");

        var dataProviderFactories = new SpriteDataProviderFactories();
        dataProviderFactories.Init();
        ISpriteEditorDataProvider dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
        if (spriteRects.Length != TileSpriteNames.Length)
            throw new InvalidOperationException($"Selected texture must contain exactly {TileSpriteNames.Length} slices, but contains {spriteRects.Length}: {path}");

        foreach (SpriteRect spriteRect in spriteRects)
        {
            if (spriteRect.rect.height < spriteRect.rect.width)
                throw new InvalidOperationException($"Tile slice '{spriteRect.name}' must be at least as tall as its square top face.");

            float pivotY = 1f - spriteRect.rect.width / (2f * spriteRect.rect.height);
            spriteRect.alignment = SpriteAlignment.Custom;
            spriteRect.pivot = new Vector2(0.5f, pivotY);
        }

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
        importer.SaveAndReimport();
    }

    [MenuItem("Assets/Rename Tile Sprite Slices", true)]
    [MenuItem("Assets/Set Tile Sprite Pivots", true)]
    private static bool CanRenameSelectedSpriteSheet()
    {
        return Selection.objects.Length == 1 && Selection.activeObject is Texture2D;
    }
}
