#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LevelPositionEditor : EditorWindow
{
    private RectTransform content;
    private GameObject buttonPrefab;
    private string metaFilePath = "";
    private List<string> warnings = new List<string>();
    private Vector2 warningScroll;
    private Texture2D backgroundTexture;

    [MenuItem("Tools/Level Position Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelPositionEditor>("Level Position Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Level Position Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        content = (RectTransform)EditorGUILayout.ObjectField("Content", content, typeof(RectTransform), true);
        buttonPrefab = (GameObject)EditorGUILayout.ObjectField("Button Prefab", buttonPrefab, typeof(GameObject), false);

        EditorGUILayout.BeginHorizontal();
        metaFilePath = EditorGUILayout.TextField("Meta File", metaFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFilePanel("Select Meta File", "Assets/StreamingAssets/levels", "json");
            if (!string.IsNullOrEmpty(selected))
                metaFilePath = FileUtil.GetProjectRelativePath(selected);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Background", EditorStyles.boldLabel);
        backgroundTexture = (Texture2D)EditorGUILayout.ObjectField("Background Image", backgroundTexture, typeof(Texture2D), false);
        if (GUILayout.Button("Fit Content to Background"))
            FitContentToBackground();
        
        if (GUILayout.Button("Create Empty Meta File"))
            CreateEmptyFile();

        EditorGUILayout.Space();

        if (GUILayout.Button("Start Editing")) StartEditing();
        if (GUILayout.Button("Add New Level")) AddNewLevel();
        if (GUILayout.Button("Finish Editing")) FinishEditing();

        EditorGUILayout.Space();

        if (warnings.Count > 0)
        {
            EditorGUILayout.LabelField("Warnings", EditorStyles.boldLabel);
            warningScroll = EditorGUILayout.BeginScrollView(warningScroll, GUILayout.Height(100));
            foreach (string warning in warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }
    }

    private void CreateEmptyFile()
    {
        string path = EditorUtility.SaveFilePanel("Create Meta File", "Assets/StreamingAssets/levels", "level_meta", "json");
        if (string.IsNullOrEmpty(path)) return;

        var empty = new LevelMetaCollection
        {
            referenceScreenWidth = content != null ? content.rect.width : 1080f,
            levels = new List<LevelMeta>()
        };

        File.WriteAllText(path, JsonConvert.SerializeObject(empty, Formatting.Indented));
        metaFilePath = FileUtil.GetProjectRelativePath(path);
        AssetDatabase.Refresh();
        Debug.Log($"Created empty meta file at {metaFilePath}");
    }

    private LevelMetaCollection LoadCollection()
    {
        if (string.IsNullOrEmpty(metaFilePath) || !File.Exists(metaFilePath))
            return new LevelMetaCollection { referenceScreenWidth = content.rect.width, levels = new List<LevelMeta>() };

        string json = File.ReadAllText(metaFilePath);
        if (string.IsNullOrWhiteSpace(json))
            return new LevelMetaCollection { referenceScreenWidth = content.rect.width, levels = new List<LevelMeta>() };

        return JsonConvert.DeserializeObject<LevelMetaCollection>(json) 
               ?? new LevelMetaCollection { referenceScreenWidth = content.rect.width, levels = new List<LevelMeta>() };
    }

    private void StartEditing()
    {
        if (content == null || buttonPrefab == null) { Debug.LogError("Assign Content and Button Prefab first."); return; }

        LevelMetaCollection collection = LoadCollection();
        if (collection.levels.Count == 0) { Debug.Log("No levels found, starting fresh."); return; }

        float scale = content.rect.width / collection.referenceScreenWidth;

        foreach (LevelMeta meta in collection.levels)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, content);
            go.name = meta.levelId.ToString();
            go.GetComponent<RectTransform>().localPosition = new Vector2(
                meta.pixelX * scale,
                meta.pixelY * scale
            );
        }
    }

    private void AddNewLevel()
    {
        if (content == null || buttonPrefab == null) { Debug.LogError("Assign Content and Button Prefab first."); return; }

        int maxId = 0;
        Vector3 lastPos = Vector3.zero;

        foreach (RectTransform child in content)
        {
            if (int.TryParse(child.name, out int id) && id > maxId)
            {
                maxId = id;
                lastPos = child.localPosition;
            }
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, content);
        go.name = (maxId + 1).ToString();
        go.GetComponent<RectTransform>().localPosition = lastPos;
    }

    private void FinishEditing()
    {
        if (content == null) { Debug.LogError("Assign Content first."); return; }
        if (string.IsNullOrEmpty(metaFilePath)) { Debug.LogError("No meta file selected."); return; }

        warnings.Clear();
        var levels = new List<LevelMeta>();
        var seenIds = new HashSet<int>();

        foreach (RectTransform child in content)
        {
            if (!int.TryParse(child.name, out int id))
            {
                warnings.Add($"'{child.name}' — name is not a number");
                continue;
            }

            if (seenIds.Contains(id))
            {
                warnings.Add($"'{child.name}' — duplicate ID {id}");
                continue;
            }

            seenIds.Add(id);
            levels.Add(new LevelMeta
            {
                levelId = id,
                levelName = $"Level {id}",
                pixelX = child.localPosition.x,
                pixelY = child.localPosition.y
            });
        }

        levels = levels.OrderBy(l => l.levelId).ToList();

        var collection = new LevelMetaCollection
        {
            referenceScreenWidth = content.rect.width,
            levels = levels
        };

        File.WriteAllText(metaFilePath, JsonConvert.SerializeObject(collection, Formatting.Indented));
        AssetDatabase.Refresh();

        List<GameObject> toDelete = new List<GameObject>();
        foreach (RectTransform child in content)
            if (int.TryParse(child.name, out _))
                toDelete.Add(child.gameObject);

        foreach (GameObject go in toDelete)
            DestroyImmediate(go);
        
        Transform bgPreview = content.Find("__BG_PREVIEW__");
        if (bgPreview != null) DestroyImmediate(bgPreview.gameObject);
        Debug.Log($"Saved {levels.Count} levels. {warnings.Count} warnings.");
    }
    
    private void FitContentToBackground()
    {
        if (content == null) { Debug.LogError("Assign Content first."); return; }
        if (backgroundTexture == null) { Debug.LogError("Assign Background Image first."); return; }

        float ratio = backgroundTexture.height / (float)backgroundTexture.width;
        float contentHeight = content.rect.width * ratio;
        content.sizeDelta = new Vector2(0, contentHeight);

        // remove existing preview bg
        Transform existing = content.Find("__BG_PREVIEW__");
        if (existing != null) DestroyImmediate(existing.gameObject);

        // create new preview
        GameObject bg = new GameObject("__BG_PREVIEW__");
        bg.transform.SetParent(content, false);
        bg.transform.SetAsFirstSibling();

        RawImage raw = bg.AddComponent<RawImage>();
        raw.texture = backgroundTexture;

        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Debug.Log($"Content resized to {content.rect.width} x {contentHeight}");
    }
}
#endif