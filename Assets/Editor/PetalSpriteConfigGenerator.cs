using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using DefaultNamespace;

public class PetalSpriteConfigGenerator : EditorWindow
{
    private string petalNamesRaw = "";
    private PetalSpriteConfig targetConfig;
    private string spriteSheetPath = "Assets/Image/PetalSpriteSheet.png";
    private Dictionary<SpecialSkillType, bool> skillToggles = new();

    private const string DefaultConfigPath = "Assets/Configs/PetalSpriteConfig.asset";

    [MenuItem("Tools/Petal Sprite Config Generator")]
    public static void ShowWindow() =>
        GetWindow<PetalSpriteConfigGenerator>("Petal Config Generator");

    private void OnEnable()
    {
        skillToggles.Clear();
        foreach (SpecialSkillType skill in Enum.GetValues(typeof(SpecialSkillType)))
        {
            if (skill == SpecialSkillType.None) continue;
            skillToggles[skill] = false;
        }

        if (targetConfig == null)
            targetConfig = AssetDatabase.LoadAssetAtPath<PetalSpriteConfig>(DefaultConfigPath);
    }

    private void OnGUI()
    {
        GUILayout.Label("Target Config", EditorStyles.boldLabel);
        targetConfig = (PetalSpriteConfig)EditorGUILayout.ObjectField(targetConfig, typeof(PetalSpriteConfig), false);

        GUILayout.Space(10);
        GUILayout.Label("Sprite Sheet Path", EditorStyles.boldLabel);
        spriteSheetPath = EditorGUILayout.TextField(spriteSheetPath);

        GUILayout.Space(10);
        GUILayout.Label("Petal Names (one per line)", EditorStyles.boldLabel);
        petalNamesRaw = EditorGUILayout.TextArea(petalNamesRaw, GUILayout.Height(100));

        GUILayout.Space(10);
        GUILayout.Label("Special Skills", EditorStyles.boldLabel);
        var keys = new List<SpecialSkillType>(skillToggles.Keys);
        foreach (var key in keys)
            skillToggles[key] = EditorGUILayout.Toggle(key.ToString(), skillToggles[key]);

        GUILayout.Space(10);
        if (GUILayout.Button("Generate")) Generate();
    }

    private void Generate()
    {
        if (targetConfig == null)
        {
            Debug.LogError("No PetalSpriteConfig assigned.");
            return;
        }

        // Load all sprites from sheet
        var allSprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
        var spriteMap = new Dictionary<string, Sprite>();
        foreach (var asset in allSprites)
        {
            if (asset is Sprite sprite)
                spriteMap[sprite.name] = sprite;
        }

        var newEntries = new List<PetalSpriteConfig.PetalSpritePair>(
            targetConfig.entries ?? new PetalSpriteConfig.PetalSpritePair[0]);

        foreach (var rawName in petalNamesRaw.Split('\n'))
        {
            string petalName = rawName.Trim();
            if (string.IsNullOrEmpty(petalName)) continue;

            // Default (no skill)
            AddEntry(newEntries, petalName, SpecialSkillType.None, spriteMap, $"{petalName}_Default");

            // Per skill
            foreach (var kvp in skillToggles)
            {
                if (!kvp.Value) continue;
                string spriteName = $"{petalName}_{kvp.Key}";
                AddEntry(newEntries, petalName, kvp.Key, spriteMap, spriteName);
            }
        }

        targetConfig.entries = newEntries.ToArray();
        EditorUtility.SetDirty(targetConfig);
        AssetDatabase.SaveAssets();
        Debug.Log($"Entries added to {targetConfig.name}.");
    }

    private void AddEntry(List<PetalSpriteConfig.PetalSpritePair> entries, string petalName,
        SpecialSkillType skill, Dictionary<string, Sprite> spriteMap, string spriteName)
    {
        if (!Enum.TryParse(petalName, out PetalType petalType))
        {
            Debug.LogWarning($"PetalType not found for: {petalName}");
            return;
        }

        spriteMap.TryGetValue(spriteName, out Sprite sprite);
        if (sprite == null)
            Debug.LogWarning($"Sprite not found: {spriteName}");

        entries.Add(new PetalSpriteConfig.PetalSpritePair
        {
            petalType = petalType,
            skillType = skill,
            sprite    = sprite
        });
    }
}