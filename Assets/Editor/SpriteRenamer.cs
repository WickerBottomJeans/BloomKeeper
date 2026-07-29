using UnityEditor;
using UnityEngine;

public class SpriteRenamer : Editor
{
    [MenuItem("Tools/Rename Petal Sprites")]
    public static void RenameSprites()
    {
        string path = "Assets/Image/PetalSpriteSheet.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null) return;

        string[] flowers = { "Rose", "Daisy", "Bluebell", "Sunflower", "Lavender", "Clover" };
        string[] variants = { "_Default", "_StripedHorizontal", "_StripedVertical", "_Bubble", "_Butterfly" };

        SpriteMetaData[] metaData = importer.spritesheet;

        int index = 0;
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                if (index < metaData.Length)
                {
                    metaData[index].name = flowers[col] + variants[row];
                    index++;
                }
            }
        }

        importer.spritesheet = metaData;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
}
