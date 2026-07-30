using System;
using System.Linq;
using DefaultNamespace;
using DefaultNamespace.Utility;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class SpriteRenamer
{
    private static readonly PetalType[] PetalTypes =
    {
        PetalType.Strawberry,
        PetalType.Mushroom,
        PetalType.Starfruit,
        PetalType.Clover,
        PetalType.Dewdrop,
        PetalType.BerryCluster,
        PetalType.Daisy
    };

    private static readonly SpecialSkillType[] SkillTypes =
    {
        SpecialSkillType.None,
        SpecialSkillType.StripedHorizontal,
        SpecialSkillType.StripedVertical,
        SpecialSkillType.Bubble,
        SpecialSkillType.Butterfly
    };

    private static readonly TileType[] TileTypes =
    {
        TileType.Normal,
        TileType.Web,
        TileType.Inactive
    };

    [MenuItem("Assets/Rename Petal Sprite Slices", false, 2000)]
    private static void RenameSelectedSpriteSheet()
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

        SpriteRect[] spriteRectsByRow = dataProvider.GetSpriteRects()
            .OrderByDescending(spriteRect => spriteRect.rect.center.y)
            .ToArray();

        int expectedSliceCount = PetalTypes.Length * SkillTypes.Length;
        if (spriteRectsByRow.Length != expectedSliceCount)
            throw new InvalidOperationException($"Selected texture must contain exactly {expectedSliceCount} slices, but contains {spriteRectsByRow.Length}: {path}");

        var orderedSpriteRects = new SpriteRect[expectedSliceCount];
        for (int row = 0; row < SkillTypes.Length; row++)
        {
            SpriteRect[] rowSpriteRects = spriteRectsByRow
                .Skip(row * PetalTypes.Length)
                .Take(PetalTypes.Length)
                .OrderBy(spriteRect => spriteRect.rect.center.x)
                .ToArray();

            for (int column = 0; column < PetalTypes.Length; column++)
            {
                int index = row * PetalTypes.Length + column;
                orderedSpriteRects[index] = rowSpriteRects[column];
                orderedSpriteRects[index].name = SpriteKeyHelper.GetPetalSpriteKey(PetalTypes[column], SkillTypes[row]);
            }
        }

        dataProvider.SetSpriteRects(orderedSpriteRects);
        dataProvider.Apply();
        importer.SaveAndReimport();
    }

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

        if (spriteRects.Length != TileTypes.Length)
            throw new InvalidOperationException($"Selected texture must contain exactly {TileTypes.Length} slices, but contains {spriteRects.Length}: {path}");

        for (int index = 0; index < TileTypes.Length; index++)
            spriteRects[index].name = SpriteKeyHelper.GetTileSpriteKey(TileTypes[index]);

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
        if (spriteRects.Length != TileTypes.Length)
            throw new InvalidOperationException($"Selected texture must contain exactly {TileTypes.Length} slices, but contains {spriteRects.Length}: {path}");

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

    [MenuItem("Assets/Rename Petal Sprite Slices", true)]
    [MenuItem("Assets/Rename Tile Sprite Slices", true)]
    [MenuItem("Assets/Set Tile Sprite Pivots", true)]
    private static bool CanRenameSelectedSpriteSheet()
    {
        return Selection.objects.Length == 1 && Selection.activeObject is Texture2D;
    }
}
