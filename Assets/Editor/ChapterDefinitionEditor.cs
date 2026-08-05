#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace;
using DefaultNamespace.UI;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace DefaultNamespace.Editor   
{
    public class ChapterDefinitionEditor : EditorWindow
    {
        private const string ChapterIndexPath = "ServerData/configs/chapters.json";
        private const string ChapterDirectoryPath = "ServerData/configs/chapters";
        private const string ConfigOutputRootPath = "ServerData/configs/";
        private readonly List<Texture2D> backgroundChunks = new List<Texture2D>();
        private readonly List<Texture2D> backgroundChunkPreviews = new List<Texture2D>();
        private readonly List<ChapterLevelDisplayData> levelButtons = new List<ChapterLevelDisplayData>();
        private ChapterTopperView topperPrefab;
        private ChapterBottomView _bottomPrefab;
        private LevelButton levelButtonPrefab;
        private Sprite chooserImage;
        private int draggedLevelIndex = -1;
        private bool isPlacingLevel;
        private Vector2 mapPreviewScroll;
        private int newChapterId = 1;
        private string newChapterName = "";
        private string chapterDescription = "";
        private string chapterDownloadLabel = "chapter_1";
        private int pendingLevelId = 1;
        private string pendingLevelName = "1";
        private int selectedLevelIndex = -1;
        private string existingChapterPath = "";
        private Vector2 chunkListScroll;

        private class ChunkRegistration
        {
            public string address;
            public AddressableAssetEntry entryToReplace;
            public Texture2D texture;
            public string previewAddress;
            public AddressableAssetEntry previewEntryToReplace;
            public Texture2D previewTexture;
        }

        [MenuItem("Tools/Chapter Definition Editor")]
        public static void ShowWindow()
        {
            ChapterDefinitionEditor window = GetWindow<ChapterDefinitionEditor>("Chapter Definition Editor");
            window.minSize = new Vector2(900f, 600f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Chapter Definition Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawChapterSidebar();
            DrawMapWorkspace();
            DrawLevelSidebar();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawChapterSidebar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(290f), GUILayout.ExpandHeight(true));
            DrawChapterFileField();
            bool hasExistingChapter = !string.IsNullOrWhiteSpace(existingChapterPath);
            if (!hasExistingChapter)
            {
                EditorGUILayout.HelpBox("No existing chapter selected. Saving will create a chapter JSON; empty chunk and level lists are allowed while authoring.", MessageType.Info);
                int previousChapterId = newChapterId;
                newChapterId = EditorGUILayout.IntField("Chapter ID", newChapterId);
                if (newChapterId != previousChapterId && chapterDownloadLabel == $"chapter_{previousChapterId}") chapterDownloadLabel = $"chapter_{newChapterId}";
            }
            newChapterName = EditorGUILayout.TextField("Chapter Name", newChapterName);
            chapterDownloadLabel = EditorGUILayout.TextField("Label", chapterDownloadLabel);
            EditorGUILayout.LabelField("Description");
            chapterDescription = EditorGUILayout.TextArea(chapterDescription, GUILayout.MinHeight(54f));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chapter Chooser Image", EditorStyles.boldLabel);
            chooserImage = (Sprite)EditorGUILayout.ObjectField(chooserImage, typeof(Sprite), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Background Chunks (bottom to top)", EditorStyles.boldLabel);
            DrawTextureDropArea();
            DrawChunkList();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Warning: removing all chunk data immediately clears the selected chapter JSON and removes this chapter's Addressables registrations. Source PNG files are not deleted.", MessageType.Warning);
            if (GUILayout.Button("Remove All Chunk Data (Includes Addressables)")) RemoveAllChunkData();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Button Prefab", EditorStyles.boldLabel);
            levelButtonPrefab = (LevelButton)EditorGUILayout.ObjectField(levelButtonPrefab, typeof(LevelButton), false);
            if (GUILayout.Button("Update Level Button Prefab (Includes Addressables)")) UpdateLevelButtonPrefab();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chapter UI Prefabs", EditorStyles.boldLabel);
            topperPrefab = (ChapterTopperView)EditorGUILayout.ObjectField("Topper", topperPrefab, typeof(ChapterTopperView), false);
            _bottomPrefab = (ChapterBottomView)EditorGUILayout.ObjectField("Bottom Navigation", _bottomPrefab, typeof(ChapterBottomView), false);
            if (GUILayout.Button("Update Chapter UI Prefabs (Includes Addressables)")) UpdateChapterUiPrefabs();

            EditorGUILayout.Space();
            if (GUILayout.Button(hasExistingChapter ? "Save Chapter" : "Create Chapter JSON")) SaveChapter();
            if (GUILayout.Button("Update Chunk Data (Includes Addressables)")) UpdateChunkData();
            EditorGUILayout.EndVertical();
        }

        private void DrawMapWorkspace()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Map Preview", EditorStyles.boldLabel);
            if (backgroundChunks.Count == 0)
                EditorGUILayout.HelpBox("Add background chunks to preview the chapter map.", MessageType.Info);
            else
                DrawMapPreview();
            EditorGUILayout.EndVertical();
        }

        private void DrawChapterFileField()
        {
            EditorGUILayout.LabelField("Existing Chapter JSON", EditorStyles.boldLabel);
            Rect dropArea = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, string.IsNullOrWhiteSpace(existingChapterPath) ? "Drop a chapter JSON here" : existingChapterPath);
            HandleChapterFileDrop(dropArea);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse")) BrowseForChapterFile();
            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(existingChapterPath));
            if (GUILayout.Button("ClearPoolAndConfig")) existingChapterPath = "";
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void HandleChapterFileDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            if (!dropArea.Contains(currentEvent.mousePosition)) return;
            if (currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform) return;

            bool hasSingleJson = DragAndDrop.paths.Length == 1 && string.Equals(Path.GetExtension(DragAndDrop.paths[0]), ".json", StringComparison.OrdinalIgnoreCase);
            DragAndDrop.visualMode = hasSingleJson ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            if (currentEvent.type == EventType.DragPerform && hasSingleJson)
            {
                DragAndDrop.AcceptDrag();
                AssignChapterPath(DragAndDrop.paths[0]);
            }
            currentEvent.Use();
        }

        private void BrowseForChapterFile()
        {
            string selectedPath = EditorUtility.OpenFilePanel("Select Chapter JSON", Path.GetFullPath(ChapterDirectoryPath), "json");
            if (string.IsNullOrWhiteSpace(selectedPath)) return;

            try
            {
                AssignChapterPath(selectedPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void AssignChapterPath(string path)
        {
            string chapterPath = Path.IsPathRooted(path) ? FileUtil.GetProjectRelativePath(path) : path.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(chapterPath) || !chapterPath.StartsWith($"{ChapterDirectoryPath}/", StringComparison.Ordinal))
                throw new InvalidOperationException($"Chapter JSON files must be inside '{ChapterDirectoryPath}'.");
            if (!string.Equals(Path.GetExtension(chapterPath), ".json", StringComparison.Ordinal) || !File.Exists(chapterPath))
                throw new InvalidOperationException($"'{chapterPath}' is not an existing JSON file.");

            LoadChapterForEditing(chapterPath);
            existingChapterPath = chapterPath;
        }

        private void LoadChapterForEditing(string chapterPath)
        {
            ChapterDefinition chapter = JsonConvert.DeserializeObject<ChapterDefinition>(File.ReadAllText(chapterPath));
            if (chapter == null)
                throw new InvalidDataException($"'{chapterPath}' does not contain a chapter definition.");
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("The project does not have active Addressables settings.");

            var loadedTextures = new List<Texture2D>();
            var loadedPreviewTextures = new List<Texture2D>();
            if (chapter.backgroundChunks != null)
            {
                foreach (ChapterBackgroundChunkData chunk in chapter.backgroundChunks)
                {
                    AddressableAssetEntry entry = FindEntryByAddress(settings, chunk.address);
                    Texture2D texture = entry == null ? FindTextureByAddressAssetName(chunk.address) : AssetDatabase.LoadAssetAtPath<Texture2D>(entry.AssetPath);
                    if (entry == null && texture != null)
                        Debug.LogWarning($"Chapter {chapter.chapterId} references missing Addressables entry '{chunk.address}'. Recovered source texture '{AssetDatabase.GetAssetPath(texture)}'; save the chapter to recreate the entry.");
                    if (entry == null && texture == null)
                        Debug.LogWarning($"Chapter {chapter.chapterId} references missing Addressables entry '{chunk.address}', and no matching source texture exists. Assign or remove its empty chunk slot before saving.");
                    loadedTextures.Add(texture);

                    if (string.IsNullOrWhiteSpace(chunk.previewAddress))
                    {
                        loadedPreviewTextures.Add(null);
                        continue;
                    }

                    AddressableAssetEntry previewEntry = FindEntryByAddress(settings, chunk.previewAddress);
                    Texture2D previewTexture = previewEntry == null ? FindTextureByAddressAssetName(chunk.previewAddress) : AssetDatabase.LoadAssetAtPath<Texture2D>(previewEntry.AssetPath);
                    if (previewEntry == null && previewTexture != null)
                        Debug.LogWarning($"Chapter {chapter.chapterId} references missing preview Addressables entry '{chunk.previewAddress}'. Recovered source texture '{AssetDatabase.GetAssetPath(previewTexture)}'; update chunk data to recreate the entry.");
                    if (previewEntry == null && previewTexture == null)
                        Debug.LogWarning($"Chapter {chapter.chapterId} references missing preview Addressables entry '{chunk.previewAddress}', and no matching source texture exists. Assign its preview before updating chunk data.");
                    loadedPreviewTextures.Add(previewTexture);
                }
            }

            backgroundChunks.Clear();
            backgroundChunks.AddRange(loadedTextures);
            backgroundChunkPreviews.Clear();
            backgroundChunkPreviews.AddRange(loadedPreviewTextures);
            topperPrefab = null;
            if (!string.IsNullOrWhiteSpace(chapter.topperPrefabAddress))
            {
                AddressableAssetEntry topperEntry = FindEntryByAddress(settings, chapter.topperPrefabAddress);
                if (topperEntry == null)
                    Debug.LogWarning($"Chapter {chapter.chapterId} references missing topper Addressables entry '{chapter.topperPrefabAddress}'. Assign the prefab and update its Addressables data.");
                else
                {
                    GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(topperEntry.AssetPath);
                    if (prefabObject == null)
                        throw new InvalidDataException($"Addressables entry '{chapter.topperPrefabAddress}' does not point to a prefab asset.");
                    topperPrefab = prefabObject.GetComponent<ChapterTopperView>();
                    if (topperPrefab == null)
                        throw new InvalidDataException($"Addressables entry '{chapter.topperPrefabAddress}' points to a prefab without a ChapterTopperView component on its root.");
                }
            }
            _bottomPrefab = null;
            if (!string.IsNullOrWhiteSpace(chapter.bottomNavigationPrefabAddress))
            {
                AddressableAssetEntry bottomEntry = FindEntryByAddress(settings, chapter.bottomNavigationPrefabAddress);
                if (bottomEntry == null)
                    Debug.LogWarning($"Chapter {chapter.chapterId} references missing bottom navigation Addressables entry '{chapter.bottomNavigationPrefabAddress}'. Assign the prefab and update its Addressables data.");
                else
                {
                    GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(bottomEntry.AssetPath);
                    if (prefabObject == null)
                        throw new InvalidDataException($"Addressables entry '{chapter.bottomNavigationPrefabAddress}' does not point to a prefab asset.");
                    _bottomPrefab = prefabObject.GetComponent<ChapterBottomView>();
                    if (_bottomPrefab == null)
                        throw new InvalidDataException($"Addressables entry '{chapter.bottomNavigationPrefabAddress}' points to a prefab without a ChapterBottomView component on its root.");
                }
            }
            levelButtonPrefab = null;
            if (!string.IsNullOrWhiteSpace(chapter.levelButtonPrefabAddress))
            {
                AddressableAssetEntry prefabEntry = FindEntryByAddress(settings, chapter.levelButtonPrefabAddress);
                if (prefabEntry == null)
                    Debug.LogWarning($"Chapter {chapter.chapterId} references missing level button Addressables entry '{chapter.levelButtonPrefabAddress}'. Assign the prefab and update its Addressables data.");
                else
                {
                    GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabEntry.AssetPath);
                    levelButtonPrefab = prefabObject.GetComponent<LevelButton>();
                    if (levelButtonPrefab == null)
                        throw new InvalidDataException($"Addressables entry '{chapter.levelButtonPrefabAddress}' points to a prefab without a LevelButton component on its root.");
                }
            }
            LoadChooserImage(chapter.chapterId, settings);
            levelButtons.Clear();
            if (chapter.levels != null) levelButtons.AddRange(chapter.levels);
            newChapterId = chapter.chapterId;
            newChapterName = chapter.chapterName;
            chapterDownloadLabel = chapter.downloadLabel;
            selectedLevelIndex = -1;
            draggedLevelIndex = -1;
            isPlacingLevel = false;
            pendingLevelId = levelButtons.Count == 0 ? 1 : levelButtons.Max(level => level.levelId) + 1;
            pendingLevelName = pendingLevelId.ToString();
        }

        private static Texture2D FindTextureByAddressAssetName(string address)
        {
            string assetName = Path.GetFileName(address);
            string[] matchingPaths = AssetDatabase.FindAssets($"{assetName} t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).Where(path => string.Equals(Path.GetFileNameWithoutExtension(path), assetName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matchingPaths.Length > 1)
                throw new InvalidDataException($"Address '{address}' has no Addressables entry and matches multiple source textures. Restore the entry or give the textures unique names.");
            return matchingPaths.Length == 1 ? AssetDatabase.LoadAssetAtPath<Texture2D>(matchingPaths[0]) : null;
        }

        private void DrawTextureDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 64f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag textures here in bottom-to-top order");

            Event currentEvent = Event.current;
            if (!dropArea.Contains(currentEvent.mousePosition)) return;
            if (currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (!(draggedObject is Texture2D texture) || backgroundChunks.Contains(texture)) continue;
                    backgroundChunks.Add(texture);
                    backgroundChunkPreviews.Add(null);
                }
            }

            currentEvent.Use();
        }

        private void DrawChunkList()
        {
            chunkListScroll = EditorGUILayout.BeginScrollView(chunkListScroll, GUILayout.MinHeight(120f), GUILayout.MaxHeight(320f));
            for (int index = 0; index < backgroundChunks.Count; index++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Chunk {index + 1}", EditorStyles.boldLabel);
                backgroundChunks[index] = (Texture2D)EditorGUILayout.ObjectField("High Resolution", backgroundChunks[index], typeof(Texture2D), false);
                backgroundChunkPreviews[index] = (Texture2D)EditorGUILayout.ObjectField("Preview", backgroundChunkPreviews[index], typeof(Texture2D), false);
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginDisabledGroup(index == 0);
                if (GUILayout.Button("Up", GUILayout.Width(42f))) MoveChunk(index, index - 1);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(index == backgroundChunks.Count - 1);
                if (GUILayout.Button("Down", GUILayout.Width(48f))) MoveChunk(index, index + 1);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                {
                    backgroundChunks.RemoveAt(index);
                    backgroundChunkPreviews.RemoveAt(index);
                    index--;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelSidebar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(250f), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Level Buttons", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(levelButtons.Count == 0);
            if (GUILayout.Button("Remove All Levels")) RemoveAllLevels();
            EditorGUI.EndDisabledGroup();

            if (backgroundChunks.Count == 0)
            {
                EditorGUILayout.HelpBox("Add background chunks before placing level buttons.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            pendingLevelId = EditorGUILayout.IntField("New Level ID", pendingLevelId);
            pendingLevelName = EditorGUILayout.TextField("Label", pendingLevelName);
            bool invalidPendingLevel = pendingLevelId <= 0 || string.IsNullOrWhiteSpace(pendingLevelName) || levelButtons.Any(level => level.levelId == pendingLevelId);
            EditorGUI.BeginDisabledGroup(invalidPendingLevel);
            if (GUILayout.Button(isPlacingLevel ? "Cancel Placement" : "Place Level")) isPlacingLevel = !isPlacingLevel;
            EditorGUI.EndDisabledGroup();

            if (isPlacingLevel) EditorGUILayout.HelpBox($"Click the map to place level {pendingLevelId}.", MessageType.Info);
            DrawSelectedLevelEditor();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Placed Levels: {levelButtons.Count}");
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedLevelEditor()
        {
            if (selectedLevelIndex < 0 || selectedLevelIndex >= levelButtons.Count) return;

            ChapterLevelDisplayData selectedLevel = levelButtons[selectedLevelIndex];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Selected Level", EditorStyles.boldLabel);
            selectedLevel.levelId = EditorGUILayout.IntField("Level ID", selectedLevel.levelId);
            selectedLevel.levelName = EditorGUILayout.TextField("Label", selectedLevel.levelName);
            EditorGUILayout.LabelField("Map Position", $"({selectedLevel.pixelX:F1}, {selectedLevel.pixelY:F1})");
            if (GUILayout.Button("Remove Selected Level"))
            {
                levelButtons.RemoveAt(selectedLevelIndex);
                selectedLevelIndex = -1;
                draggedLevelIndex = -1;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMapPreview()
        {
            if (backgroundChunks[0] == null)
            {
                EditorGUILayout.HelpBox("The first background chunk is missing. Assign or remove its empty slot before using the map preview.", MessageType.Error);
                return;
            }

            float mapPixelWidth = backgroundChunks[0].width;

            float previewWidth = Mathf.Max(position.width - 580f, 320f);
            float scale = previewWidth / mapPixelWidth;
            float mapHeight = CalculateMapHeight(mapPixelWidth);
            float previewHeight = mapHeight * scale;

            mapPreviewScroll = EditorGUILayout.BeginScrollView(mapPreviewScroll, GUILayout.ExpandHeight(true));
            Rect mapRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false));
            DrawBackgroundPreview(mapRect, previewWidth);
            DrawLevelMarkers(mapRect, scale, mapHeight);
            HandleMapInteraction(mapRect, scale, mapHeight);
            EditorGUILayout.EndScrollView();
        }

        private void DrawBackgroundPreview(Rect mapRect, float previewWidth)
        {
            float currentY = mapRect.y;
            for (int index = backgroundChunks.Count - 1; index >= 0; index--)
            {
                Texture2D texture = backgroundChunks[index];
                if (texture == null) continue;
                float chunkHeight = previewWidth * (texture.height / (float)texture.width);
                GUI.DrawTexture(new Rect(mapRect.x, currentY, previewWidth, chunkHeight), texture, ScaleMode.StretchToFill);
                currentY += chunkHeight;
            }
        }

        private void DrawLevelMarkers(Rect mapRect, float scale, float mapHeight)
        {
            for (int index = 0; index < levelButtons.Count; index++)
            {
                ChapterLevelDisplayData level = levelButtons[index];
                Vector2 positionOnMap = MapPositionToPreview(level, mapRect, scale, mapHeight);
                Rect markerRect = new Rect(positionOnMap.x - 18f, positionOnMap.y - 18f, 36f, 36f);
                GUIStyle markerStyle = index == selectedLevelIndex ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                GUI.Box(markerRect, level.levelId.ToString(), markerStyle);
            }
        }

        private void HandleMapInteraction(Rect mapRect, float scale, float mapHeight)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && draggedLevelIndex >= 0)
            {
                draggedLevelIndex = -1;
                currentEvent.Use();
                return;
            }
            if (!mapRect.Contains(currentEvent.mousePosition)) return;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                int hitLevelIndex = FindLevelAtPreviewPosition(currentEvent.mousePosition, mapRect, scale, mapHeight);
                if (hitLevelIndex >= 0)
                {
                    selectedLevelIndex = hitLevelIndex;
                    draggedLevelIndex = hitLevelIndex;
                    isPlacingLevel = false;
                    currentEvent.Use();
                    return;
                }

                if (isPlacingLevel)
                {
                    ChapterLevelDisplayData level = CreateLevelAtPreviewPosition(currentEvent.mousePosition, mapRect, scale, mapHeight);
                    levelButtons.Add(level);
                    selectedLevelIndex = levelButtons.Count - 1;
                    pendingLevelId++;
                    pendingLevelName = pendingLevelId.ToString();
                    isPlacingLevel = false;
                    currentEvent.Use();
                }
            }
            else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && draggedLevelIndex >= 0)
            {
                SetLevelPositionFromPreview(levelButtons[draggedLevelIndex], currentEvent.mousePosition, mapRect, scale, mapHeight);
                Repaint();
                currentEvent.Use();
            }
        }

        private ChapterLevelDisplayData CreateLevelAtPreviewPosition(Vector2 previewPosition, Rect mapRect, float scale, float mapHeight)
        {
            var level = new ChapterLevelDisplayData { levelId = pendingLevelId, levelName = pendingLevelName };
            SetLevelPositionFromPreview(level, previewPosition, mapRect, scale, mapHeight);
            return level;
        }

        private void SetLevelPositionFromPreview(ChapterLevelDisplayData level, Vector2 previewPosition, Rect mapRect, float scale, float mapHeight)
        {
            float mapPixelWidth = backgroundChunks[0].width;
            level.pixelX = Mathf.Clamp((previewPosition.x - mapRect.x) / scale - mapPixelWidth / 2f, -mapPixelWidth / 2f, mapPixelWidth / 2f);
            level.pixelY = Mathf.Clamp(mapHeight - (previewPosition.y - mapRect.y) / scale, 0f, mapHeight);
        }

        private int FindLevelAtPreviewPosition(Vector2 previewPosition, Rect mapRect, float scale, float mapHeight)
        {
            for (int index = levelButtons.Count - 1; index >= 0; index--)
                if (Vector2.Distance(previewPosition, MapPositionToPreview(levelButtons[index], mapRect, scale, mapHeight)) <= 20f) return index;
            return -1;
        }

        private static Vector2 MapPositionToPreview(ChapterLevelDisplayData level, Rect mapRect, float scale, float mapHeight)
        {
            float previewX = mapRect.x + mapRect.width / 2f + level.pixelX * scale;
            float previewY = mapRect.y + (mapHeight - level.pixelY) * scale;
            return new Vector2(previewX, previewY);
        }

        private float CalculateMapHeight(float mapPixelWidth)
        {
            float height = 0f;
            foreach (Texture2D texture in backgroundChunks)
                if (texture != null) height += mapPixelWidth * (texture.height / (float)texture.width);
            return height;
        }

        private void MoveChunk(int sourceIndex, int destinationIndex)
        {
            Texture2D texture = backgroundChunks[sourceIndex];
            Texture2D previewTexture = backgroundChunkPreviews[sourceIndex];
            backgroundChunks.RemoveAt(sourceIndex);
            backgroundChunkPreviews.RemoveAt(sourceIndex);
            backgroundChunks.Insert(destinationIndex, texture);
            backgroundChunkPreviews.Insert(destinationIndex, previewTexture);
        }

        private void SaveChapter()
        {
            try
            {
                bool isCreating = string.IsNullOrWhiteSpace(existingChapterPath);
                string chapterPath;
                ChapterDefinition chapter;
                if (isCreating)
                {
                    chapterPath = $"{ChapterDirectoryPath}/chapter_{newChapterId}.json";
                    if (File.Exists(chapterPath))
                        throw new InvalidOperationException($"Chapter file '{chapterPath}' already exists. Select it as the existing chapter instead of overwriting it.");

                    chapter = new ChapterDefinition
                    {
                        chapterId = newChapterId,
                        chapterName = newChapterName,
                        downloadLabel = chapterDownloadLabel,
                        topperPrefabAddress = "",
                        bottomNavigationPrefabAddress = "",
                        levelButtonPrefabAddress = "",
                        levels = new List<ChapterLevelDisplayData>(),
                        backgroundChunks = new List<ChapterBackgroundChunkData>()
                    };
                }
                else
                {
                    chapterPath = existingChapterPath;
                    if (string.IsNullOrWhiteSpace(chapterPath) || Path.GetExtension(chapterPath) != ".json")
                        throw new InvalidOperationException("Select a chapter JSON asset before updating background chunks.");

                    chapter = JsonConvert.DeserializeObject<ChapterDefinition>(File.ReadAllText(chapterPath));
                    if (chapter == null)
                        throw new InvalidDataException($"'{chapterPath}' does not contain a chapter definition.");
                }

                chapter.chapterName = newChapterName;
                if (chapter.chapterId <= 0)
                    throw new InvalidDataException("Chapter identity must be positive.");
                if (string.IsNullOrWhiteSpace(chapter.chapterName))
                    throw new InvalidDataException($"Chapter {chapter.chapterId} must have a name.");

                chapter.downloadLabel = chapterDownloadLabel;
                ValidateChapterDownloadLabel(chapter);
                chapter.levels = levelButtons.OrderBy(level => level.levelId).ToList();
                ValidateLevelButtons(chapter);
                chapter.backgroundChunks = BuildChunkData(chapter.chapterId);
                string directory = Path.GetDirectoryName(chapterPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                string chooserImageAddress = RegisterChooserImage(chapter);
                File.WriteAllText(chapterPath, JsonConvert.SerializeObject(chapter, Formatting.Indented));
                UpdateChapterIndex(chapter, chapterPath, chooserImageAddress, chapterDescription);
                ApplyChapterLabelToExistingEntries(chapter.downloadLabel);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (isCreating) existingChapterPath = chapterPath;
                Debug.Log($"{(isCreating ? "Created" : "Updated")} chapter {chapter.chapterId} with {chapter.backgroundChunks.Count} background chunks and {chapter.levels.Count} level buttons at '{chapterPath}'.");
            }
            catch (OperationCanceledException exception)
            {
                Debug.Log(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void LoadChooserImage(int chapterId, AddressableAssetSettings settings)
        {
            chooserImage = null;
            chapterDescription = "";
            if (!File.Exists(ChapterIndexPath)) return;

            ChapterIndex index = JsonConvert.DeserializeObject<ChapterIndex>(File.ReadAllText(ChapterIndexPath));
            if (index == null || index.chapters == null) throw new InvalidDataException($"'{ChapterIndexPath}' does not contain a chapter index.");
            ChapterIndexEntry indexEntry = index.chapters.SingleOrDefault(entry => entry.chapterId == chapterId);
            if (indexEntry == null) return;
            chapterDescription = indexEntry.description ?? "";
            if (string.IsNullOrWhiteSpace(indexEntry.chooserImageAddress)) return;

            AddressableAssetEntry addressableEntry = FindEntryByAddress(settings, indexEntry.chooserImageAddress);
            if (addressableEntry == null)
                throw new InvalidDataException($"Chapter {chapterId} references missing chooser image Addressables entry '{indexEntry.chooserImageAddress}'.");
            chooserImage = AssetDatabase.LoadAssetAtPath<Sprite>(addressableEntry.AssetPath);
            if (chooserImage == null)
                throw new InvalidDataException($"Addressables entry '{indexEntry.chooserImageAddress}' does not point to a Sprite asset.");
        }

        private string RegisterChooserImage(ChapterDefinition chapter)
        {
            if (chooserImage == null) throw new InvalidOperationException("Assign a chapter chooser image before saving the chapter.");
            string imagePath = AssetDatabase.GetAssetPath(chooserImage);
            if (string.IsNullOrWhiteSpace(imagePath)) throw new InvalidOperationException("The assigned chapter chooser image must be a project asset.");

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("The project does not have active Addressables settings.");

            string imageGuid = AssetDatabase.AssetPathToGUID(imagePath);
            string address = $"chapter/{chapter.chapterId}/ui/chooser-image";
            AddressableAssetEntry addressOwner = FindEntryByAddress(settings, address);
            if (addressOwner != null && addressOwner.guid != imageGuid && !EditorUtility.DisplayDialog("Replace Addressable Asset", $"Address '{address}' is currently assigned to '{addressOwner.AssetPath}'.\n\nReplace it with '{imagePath}'?", "Replace", "Cancel"))
                throw new OperationCanceledException("Chapter chooser image Addressables update was cancelled without making changes.");

            ChapterIndex index = LoadOrCreateChapterIndex();
            ChapterIndexEntry previousIndexEntry = index.chapters.SingleOrDefault(entry => entry.chapterId == chapter.chapterId);
            AddressableAssetEntry previousAddressableEntry = previousIndexEntry == null || string.IsNullOrWhiteSpace(previousIndexEntry.chooserImageAddress) ? null : FindEntryByAddress(settings, previousIndexEntry.chooserImageAddress);
            if (addressOwner != null && addressOwner.guid != imageGuid) settings.RemoveAssetEntry(addressOwner.guid);
            if (previousAddressableEntry != null && previousAddressableEntry.guid != imageGuid && previousAddressableEntry != addressOwner) settings.RemoveAssetEntry(previousAddressableEntry.guid);

            AddressableAssetGroup uiGroup = GetOrCreateChapterUiGroup(settings, chapter.chapterId);
            AddressableAssetEntry imageEntry = settings.CreateOrMoveEntry(imageGuid, uiGroup);
            imageEntry.address = address;
            ApplyChapterDownloadLabel(imageEntry, chapter.downloadLabel);
            EditorUtility.SetDirty(settings);
            return address;
        }

        private static void UpdateChapterIndex(ChapterDefinition chapter, string chapterPath, string chooserImageAddress, string description)
        {
            ChapterIndex index = LoadOrCreateChapterIndex();
            ChapterIndexEntry entry = index.chapters.SingleOrDefault(candidate => candidate.chapterId == chapter.chapterId);
            if (entry == null)
            {
                entry = new ChapterIndexEntry { chapterId = chapter.chapterId };
                index.chapters.Add(entry);
            }

            if (!chapterPath.StartsWith(ConfigOutputRootPath, StringComparison.Ordinal))
                throw new InvalidOperationException($"Chapter file '{chapterPath}' must be inside '{ConfigOutputRootPath}'.");
            entry.displayName = chapter.chapterName;
            entry.description = description;
            entry.configPath = chapterPath.Substring(ConfigOutputRootPath.Length).Replace('\\', '/');
            entry.chooserImageAddress = chooserImageAddress;
            entry.downloadLabel = chapter.downloadLabel;
            entry.unlockLevelId = chapter.levels.Min(level => level.levelId);
            Directory.CreateDirectory(Path.GetDirectoryName(ChapterIndexPath));
            File.WriteAllText(ChapterIndexPath, JsonConvert.SerializeObject(index, Formatting.Indented));
        }

        private static ChapterIndex LoadOrCreateChapterIndex()
        {
            if (!File.Exists(ChapterIndexPath)) return new ChapterIndex { chapters = new List<ChapterIndexEntry>() };
            ChapterIndex index = JsonConvert.DeserializeObject<ChapterIndex>(File.ReadAllText(ChapterIndexPath));
            if (index == null) throw new InvalidDataException($"'{ChapterIndexPath}' does not contain a chapter index.");
            if (index.chapters == null) index.chapters = new List<ChapterIndexEntry>();
            return index;
        }

        private static void ValidateLevelButtons(ChapterDefinition chapter)
        {
            if (chapter.levels.Count == 0)
                throw new InvalidDataException($"Chapter {chapter.chapterId} must contain at least one level.");

            var levelIds = new HashSet<int>();
            foreach (ChapterLevelDisplayData level in chapter.levels)
            {
                if (level == null || level.levelId <= 0)
                    throw new InvalidDataException($"Chapter {chapter.chapterId} contains an invalid level identity.");
                if (string.IsNullOrWhiteSpace(level.levelName))
                    throw new InvalidDataException($"Level {level.levelId} in chapter {chapter.chapterId} must have a label.");
                if (!levelIds.Add(level.levelId))
                    throw new InvalidDataException($"Chapter {chapter.chapterId} contains level {level.levelId} more than once.");
            }
        }

        private List<ChapterBackgroundChunkData> BuildChunkData(int chapterId)
        {
            if (backgroundChunkPreviews.Count != backgroundChunks.Count)
                throw new InvalidOperationException("Every background chunk must have one preview texture slot.");

            var chunkData = new List<ChapterBackgroundChunkData>(backgroundChunks.Count);
            var addresses = new HashSet<string>();
            for (int index = 0; index < backgroundChunks.Count; index++)
            {
                Texture2D texture = backgroundChunks[index];
                Texture2D previewTexture = backgroundChunkPreviews[index];
                if (texture == null)
                    throw new InvalidOperationException("The background chunk list contains an empty texture slot.");
                if (previewTexture == null)
                    throw new InvalidOperationException($"Background chunk {index + 1} does not have a preview texture.");

                string address = $"chapter/{chapterId}/background/{texture.name}";
                string previewAddress = $"chapter/{chapterId}/background-preview/{previewTexture.name}";
                if (!addresses.Add(address))
                    throw new InvalidOperationException($"More than one chunk would use address '{address}'. Rename the textures so every chunk has a unique name.");
                if (!addresses.Add(previewAddress))
                    throw new InvalidOperationException($"More than one chunk preview would use address '{previewAddress}'. Rename the preview textures so every preview has a unique name.");
                chunkData.Add(new ChapterBackgroundChunkData { address = address, previewAddress = previewAddress, width = texture.width, height = texture.height });
            }
            return chunkData;
        }

        private void UpdateLevelButtonPrefab()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(existingChapterPath))
                    throw new InvalidOperationException("Select or create the chapter JSON before updating its level button prefab.");
                if (levelButtonPrefab == null)
                    throw new InvalidOperationException("Assign a LevelButton prefab before updating its Addressables data.");

                string prefabPath = AssetDatabase.GetAssetPath(levelButtonPrefab.gameObject);
                if (string.IsNullOrWhiteSpace(prefabPath) || PrefabUtility.GetPrefabAssetType(levelButtonPrefab.gameObject) == PrefabAssetType.NotAPrefab)
                    throw new InvalidOperationException("The assigned LevelButton must belong to a prefab asset.");

                ChapterDefinition chapter = JsonConvert.DeserializeObject<ChapterDefinition>(File.ReadAllText(existingChapterPath));
                if (chapter == null)
                    throw new InvalidDataException($"'{existingChapterPath}' does not contain a chapter definition.");
                ValidateChapterDownloadLabel(chapter);

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                    throw new InvalidOperationException("The project does not have active Addressables settings.");

                string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
                string address = $"chapter/{chapter.chapterId}/ui/level-button";
                AddressableAssetEntry addressOwner = FindEntryByAddress(settings, address);
                if (addressOwner != null && addressOwner.guid != prefabGuid && !EditorUtility.DisplayDialog("Replace Addressable Asset", $"Address '{address}' is currently assigned to '{addressOwner.AssetPath}'.\n\nReplace it with '{prefabPath}'?", "Replace", "Cancel"))
                    throw new OperationCanceledException("Level button Addressables update was cancelled without making changes.");

                AddressableAssetEntry previousEntry = string.IsNullOrWhiteSpace(chapter.levelButtonPrefabAddress) ? null : FindEntryByAddress(settings, chapter.levelButtonPrefabAddress);
                if (addressOwner != null && addressOwner.guid != prefabGuid) settings.RemoveAssetEntry(addressOwner.guid);
                if (previousEntry != null && previousEntry.guid != prefabGuid && previousEntry != addressOwner) settings.RemoveAssetEntry(previousEntry.guid);

                AddressableAssetGroup chapterGroup = GetOrCreateChapterUiGroup(settings, chapter.chapterId);
                AddressableAssetEntry prefabEntry = settings.CreateOrMoveEntry(prefabGuid, chapterGroup);
                prefabEntry.address = address;
                ApplyChapterDownloadLabel(prefabEntry, chapter.downloadLabel);
                chapter.levelButtonPrefabAddress = address;
                File.WriteAllText(existingChapterPath, JsonConvert.SerializeObject(chapter, Formatting.Indented));
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Updated chapter {chapter.chapterId} level button prefab JSON and Addressables to '{address}'.");
            }
            catch (OperationCanceledException exception)
            {
                Debug.Log(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void UpdateChapterUiPrefabs()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(existingChapterPath))
                    throw new InvalidOperationException("Select or create the chapter JSON before updating its UI prefabs.");
                if (topperPrefab == null)
                    throw new InvalidOperationException("Assign a ChapterTopperView prefab before updating chapter UI Addressables data.");
                if (_bottomPrefab == null)
                    throw new InvalidOperationException("Assign a ChapterBottomView prefab before updating chapter UI Addressables data.");

                string topperPath = AssetDatabase.GetAssetPath(topperPrefab.gameObject);
                string bottomPath = AssetDatabase.GetAssetPath(_bottomPrefab.gameObject);
                if (string.IsNullOrWhiteSpace(topperPath) || PrefabUtility.GetPrefabAssetType(topperPrefab.gameObject) == PrefabAssetType.NotAPrefab)
                    throw new InvalidOperationException("The assigned ChapterTopperView must belong to a prefab asset.");
                if (string.IsNullOrWhiteSpace(bottomPath) || PrefabUtility.GetPrefabAssetType(_bottomPrefab.gameObject) == PrefabAssetType.NotAPrefab)
                    throw new InvalidOperationException("The assigned ChapterBottomView must belong to a prefab asset.");

                string topperGuid = AssetDatabase.AssetPathToGUID(topperPath);
                string bottomGuid = AssetDatabase.AssetPathToGUID(bottomPath);
                if (topperGuid == bottomGuid)
                    throw new InvalidOperationException("Topper and Bottom Navigation must be different prefab assets.");

                ChapterDefinition chapter = JsonConvert.DeserializeObject<ChapterDefinition>(File.ReadAllText(existingChapterPath));
                if (chapter == null)
                    throw new InvalidDataException($"'{existingChapterPath}' does not contain a chapter definition.");
                ValidateChapterDownloadLabel(chapter);

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                    throw new InvalidOperationException("The project does not have active Addressables settings.");

                string topperAddress = $"chapter/{chapter.chapterId}/ui/topper";
                string bottomAddress = $"chapter/{chapter.chapterId}/ui/bottom-navigation";
                AddressableAssetEntry topperAddressOwner = FindEntryByAddress(settings, topperAddress);
                AddressableAssetEntry bottomAddressOwner = FindEntryByAddress(settings, bottomAddress);
                if (topperAddressOwner != null && topperAddressOwner.guid != topperGuid && !EditorUtility.DisplayDialog("Replace Addressable Asset", $"Address '{topperAddress}' is currently assigned to '{topperAddressOwner.AssetPath}'.\n\nReplace it with '{topperPath}'?", "Replace", "Cancel"))
                    throw new OperationCanceledException("Chapter UI Addressables update was cancelled without making changes.");
                if (bottomAddressOwner != null && bottomAddressOwner.guid != bottomGuid && !EditorUtility.DisplayDialog("Replace Addressable Asset", $"Address '{bottomAddress}' is currently assigned to '{bottomAddressOwner.AssetPath}'.\n\nReplace it with '{bottomPath}'?", "Replace", "Cancel"))
                    throw new OperationCanceledException("Chapter UI Addressables update was cancelled without making changes.");

                AddressableAssetEntry previousTopperEntry = string.IsNullOrWhiteSpace(chapter.topperPrefabAddress) ? null : FindEntryByAddress(settings, chapter.topperPrefabAddress);
                AddressableAssetEntry previousBottomEntry = string.IsNullOrWhiteSpace(chapter.bottomNavigationPrefabAddress) ? null : FindEntryByAddress(settings, chapter.bottomNavigationPrefabAddress);
                if (topperAddressOwner != null && topperAddressOwner.guid != topperGuid) settings.RemoveAssetEntry(topperAddressOwner.guid);
                if (bottomAddressOwner != null && bottomAddressOwner.guid != bottomGuid) settings.RemoveAssetEntry(bottomAddressOwner.guid);
                if (previousTopperEntry != null && previousTopperEntry.guid != topperGuid && previousTopperEntry != topperAddressOwner && previousTopperEntry != bottomAddressOwner) settings.RemoveAssetEntry(previousTopperEntry.guid);
                if (previousBottomEntry != null && previousBottomEntry.guid != bottomGuid && previousBottomEntry != topperAddressOwner && previousBottomEntry != bottomAddressOwner && previousBottomEntry != previousTopperEntry) settings.RemoveAssetEntry(previousBottomEntry.guid);

                AddressableAssetGroup uiGroup = GetOrCreateChapterUiGroup(settings, chapter.chapterId);
                AddressableAssetEntry topperEntry = settings.CreateOrMoveEntry(topperGuid, uiGroup);
                topperEntry.address = topperAddress;
                ApplyChapterDownloadLabel(topperEntry, chapter.downloadLabel);
                AddressableAssetEntry bottomEntry = settings.CreateOrMoveEntry(bottomGuid, uiGroup);
                bottomEntry.address = bottomAddress;
                ApplyChapterDownloadLabel(bottomEntry, chapter.downloadLabel);
                chapter.topperPrefabAddress = topperAddress;
                chapter.bottomNavigationPrefabAddress = bottomAddress;
                File.WriteAllText(existingChapterPath, JsonConvert.SerializeObject(chapter, Formatting.Indented));
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Updated chapter {chapter.chapterId} Topper and Bottom Navigation JSON and Addressables entries.");
            }
            catch (OperationCanceledException exception)
            {
                Debug.Log(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void UpdateChunkData()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(existingChapterPath))
                    throw new InvalidOperationException("Select or create the chapter JSON before updating its chunk data.");

                ChapterDefinition chapter = JsonConvert.DeserializeObject<ChapterDefinition>(File.ReadAllText(existingChapterPath));
                if (chapter == null)
                    throw new InvalidDataException($"'{existingChapterPath}' does not contain a chapter definition.");
                ValidateChapterDownloadLabel(chapter);

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                    throw new InvalidOperationException("The project does not have active Addressables settings.");

                List<ChunkRegistration> registrations = PlanRegistrations(settings, chapter);
                AddressableAssetGroup chapterGroup = GetOrCreateChapterGroup(settings, chapter.chapterId);
                AddressableAssetGroup previewGroup = GetOrCreateChapterPreviewGroup(settings, chapter.chapterId);
                var currentTextureGuids = new HashSet<string>(registrations.SelectMany(registration => new[] { AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(registration.texture)), AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(registration.previewTexture)) }));
                List<ChapterBackgroundChunkData> chunkData = ApplyRegistrations(settings, chapterGroup, previewGroup, chapter.downloadLabel, registrations);

                string addressPrefix = $"chapter/{chapter.chapterId}/background/";
                string previewAddressPrefix = $"chapter/{chapter.chapterId}/background-preview/";
                var obsoleteEntryGuids = new HashSet<string>();
                foreach (AddressableAssetGroup group in settings.groups)
                {
                    if (group == null) continue;
                    foreach (AddressableAssetEntry entry in group.entries)
                        if ((group == chapterGroup || group == previewGroup || entry.address.StartsWith(addressPrefix, StringComparison.Ordinal) || entry.address.StartsWith(previewAddressPrefix, StringComparison.Ordinal)) && !currentTextureGuids.Contains(entry.guid)) obsoleteEntryGuids.Add(entry.guid);
                }
                foreach (string guid in obsoleteEntryGuids) settings.RemoveAssetEntry(guid);

                chapter.backgroundChunks = chunkData;
                File.WriteAllText(existingChapterPath, JsonConvert.SerializeObject(chapter, Formatting.Indented));
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Updated chapter {chapter.chapterId} JSON and Addressables to contain exactly {chunkData.Count} background chunks.");
            }
            catch (OperationCanceledException exception)
            {
                Debug.Log(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void RemoveAllLevels()
        {
            string message = string.IsNullOrWhiteSpace(existingChapterPath) ? "This removes every level button from the editor. Continue?" : "This immediately clears levels in the selected chapter JSON and removes every level button from the editor. Continue?";
            if (!EditorUtility.DisplayDialog("Remove All Levels", message, "Remove All", "Cancel")) return;

            try
            {
                if (!string.IsNullOrWhiteSpace(existingChapterPath))
                {
                    ChapterDefinition chapter = JsonConvert.DeserializeObject<ChapterDefinition>(File.ReadAllText(existingChapterPath));
                    if (chapter == null)
                        throw new InvalidDataException($"'{existingChapterPath}' does not contain a chapter definition.");
                    chapter.levels = new List<ChapterLevelDisplayData>();
                    File.WriteAllText(existingChapterPath, JsonConvert.SerializeObject(chapter, Formatting.Indented));
                    AssetDatabase.Refresh();
                }

                levelButtons.Clear();
                selectedLevelIndex = -1;
                draggedLevelIndex = -1;
                isPlacingLevel = false;
                Debug.Log(string.IsNullOrWhiteSpace(existingChapterPath) ? "Removed all level buttons from the chapter editor." : $"Cleared level data in '{existingChapterPath}'.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void RemoveAllChunkData()
        {
            if (!EditorUtility.DisplayDialog("Remove All Chunk Data", "This immediately clears backgroundChunks in the selected chapter JSON, clears the editor list, and removes this chapter's Addressables registrations. Source PNG files will not be deleted. Continue?", "Remove All", "Cancel")) return;

            try
            {
                if (string.IsNullOrWhiteSpace(existingChapterPath))
                    throw new InvalidOperationException("Select or create the chapter JSON before removing its chunk data.");

                ChapterDefinition chapter = JsonConvert.DeserializeObject<ChapterDefinition>(File.ReadAllText(existingChapterPath));
                if (chapter == null)
                    throw new InvalidDataException($"'{existingChapterPath}' does not contain a chapter definition.");

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                    throw new InvalidOperationException("The project does not have active Addressables settings.");

                string groupName = $"Chapter_{chapter.chapterId}";
                string previewGroupName = $"Chapter_{chapter.chapterId}_Preview";
                string addressPrefix = $"chapter/{chapter.chapterId}/background/";
                string previewAddressPrefix = $"chapter/{chapter.chapterId}/background-preview/";
                AddressableAssetGroup chapterGroup = settings.FindGroup(groupName);
                AddressableAssetGroup previewGroup = settings.FindGroup(previewGroupName);
                var entryGuids = new HashSet<string>();
                foreach (AddressableAssetGroup group in settings.groups)
                {
                    if (group == null) continue;
                    foreach (AddressableAssetEntry entry in group.entries)
                        if (group == chapterGroup || group == previewGroup || entry.address.StartsWith(addressPrefix, StringComparison.Ordinal) || entry.address.StartsWith(previewAddressPrefix, StringComparison.Ordinal)) entryGuids.Add(entry.guid);
                }

                foreach (string guid in entryGuids) settings.RemoveAssetEntry(guid);
                backgroundChunks.Clear();
                backgroundChunkPreviews.Clear();
                chapter.backgroundChunks = new List<ChapterBackgroundChunkData>();
                File.WriteAllText(existingChapterPath, JsonConvert.SerializeObject(chapter, Formatting.Indented));
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Cleared chapter {chapter.chapterId} background chunk JSON data and removed {entryGuids.Count} Addressables registrations.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private List<ChunkRegistration> PlanRegistrations(AddressableAssetSettings settings, ChapterDefinition chapter)
        {
            if (backgroundChunkPreviews.Count != backgroundChunks.Count)
                throw new InvalidOperationException("Every background chunk must have one preview texture slot.");

            var registrations = new List<ChunkRegistration>(backgroundChunks.Count);
            var plannedAddresses = new HashSet<string>();

            for (int index = 0; index < backgroundChunks.Count; index++)
            {
                Texture2D texture = backgroundChunks[index];
                Texture2D previewTexture = backgroundChunkPreviews[index];
                if (texture == null)
                    throw new InvalidOperationException("The background chunk list contains an empty texture slot.");
                if (previewTexture == null)
                    throw new InvalidOperationException($"Background chunk {index + 1} does not have a preview texture.");

                string texturePath = AssetDatabase.GetAssetPath(texture);
                string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
                string previewTexturePath = AssetDatabase.GetAssetPath(previewTexture);
                string previewTextureGuid = AssetDatabase.AssetPathToGUID(previewTexturePath);
                string desiredAddress = $"chapter/{chapter.chapterId}/background/{texture.name}";
                string desiredPreviewAddress = $"chapter/{chapter.chapterId}/background-preview/{previewTexture.name}";
                if (!plannedAddresses.Add(desiredAddress))
                    throw new InvalidOperationException($"More than one dragged texture would use address '{desiredAddress}'. Rename the textures so every chunk has a unique name.");
                if (!plannedAddresses.Add(desiredPreviewAddress))
                    throw new InvalidOperationException($"More than one preview texture would use address '{desiredPreviewAddress}'. Rename the preview textures so every preview has a unique name.");

                AddressableAssetEntry addressOwner = FindEntryByAddress(settings, desiredAddress);
                if (addressOwner != null && addressOwner.guid != textureGuid && !EditorUtility.DisplayDialog("Replace Addressable Asset", $"Address '{desiredAddress}' is currently assigned to '{addressOwner.AssetPath}'.\n\nReplace it with '{texturePath}'?", "Replace", "Cancel"))
                    throw new OperationCanceledException("Addressables update was cancelled without making changes.");
                AddressableAssetEntry previewAddressOwner = FindEntryByAddress(settings, desiredPreviewAddress);
                if (previewAddressOwner != null && previewAddressOwner.guid != previewTextureGuid && !EditorUtility.DisplayDialog("Replace Preview Addressable Asset", $"Address '{desiredPreviewAddress}' is currently assigned to '{previewAddressOwner.AssetPath}'.\n\nReplace it with '{previewTexturePath}'?", "Replace", "Cancel"))
                    throw new OperationCanceledException("Addressables update was cancelled without making changes.");

                registrations.Add(new ChunkRegistration { address = desiredAddress, entryToReplace = addressOwner != null && addressOwner.guid != textureGuid ? addressOwner : null, texture = texture, previewAddress = desiredPreviewAddress, previewEntryToReplace = previewAddressOwner != null && previewAddressOwner.guid != previewTextureGuid ? previewAddressOwner : null, previewTexture = previewTexture });
            }

            return registrations;
        }

        private List<ChapterBackgroundChunkData> ApplyRegistrations(AddressableAssetSettings settings, AddressableAssetGroup chapterGroup, AddressableAssetGroup previewGroup, string downloadLabel, List<ChunkRegistration> registrations)
        {
            var chunkData = new List<ChapterBackgroundChunkData>(registrations.Count);
            foreach (ChunkRegistration registration in registrations)
            {
                if (registration.entryToReplace != null) settings.RemoveAssetEntry(registration.entryToReplace.guid);
                if (registration.previewEntryToReplace != null) settings.RemoveAssetEntry(registration.previewEntryToReplace.guid);

                string texturePath = AssetDatabase.GetAssetPath(registration.texture);
                string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(textureGuid, chapterGroup);
                entry.address = registration.address;
                ApplyChapterDownloadLabel(entry, downloadLabel);

                string previewTexturePath = AssetDatabase.GetAssetPath(registration.previewTexture);
                string previewTextureGuid = AssetDatabase.AssetPathToGUID(previewTexturePath);
                AddressableAssetEntry previewEntry = settings.CreateOrMoveEntry(previewTextureGuid, previewGroup);
                previewEntry.address = registration.previewAddress;
                ApplyChapterDownloadLabel(previewEntry, downloadLabel);

                chunkData.Add(new ChapterBackgroundChunkData { address = registration.address, previewAddress = registration.previewAddress, width = registration.texture.width, height = registration.texture.height });
            }
            return chunkData;
        }

        private static void ApplyChapterDownloadLabel(AddressableAssetEntry entry, string downloadLabel)
        {
            foreach (string existingLabel in entry.labels.Where(IsChapterDownloadLabel).ToList())
                if (existingLabel != downloadLabel)
                    entry.SetLabel(existingLabel, false);
            entry.SetLabel(downloadLabel, true, true);
        }

        private void ApplyChapterLabelToExistingEntries(string chapterLabel)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("The project does not have active Addressables settings.");

            IEnumerable<UnityEngine.Object> chapterAssets = backgroundChunks.Cast<UnityEngine.Object>()
                .Concat(backgroundChunkPreviews)
                .Concat(new UnityEngine.Object[] { topperPrefab, _bottomPrefab, levelButtonPrefab });
            foreach (UnityEngine.Object chapterAsset in chapterAssets)
            {
                if (chapterAsset == null) continue;
                string assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(chapterAsset));
                AddressableAssetEntry entry = settings.FindAssetEntry(assetGuid);
                if (entry != null) ApplyChapterDownloadLabel(entry, chapterLabel);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void ValidateChapterDownloadLabel(ChapterDefinition chapter)
        {
            if (string.IsNullOrWhiteSpace(chapter.downloadLabel))
                throw new InvalidDataException($"Chapter {chapter.chapterId} must have a download label before updating its Addressables entries.");
        }

        private static bool IsChapterDownloadLabel(string label)
        {
            const string prefix = "chapter_";
            return label.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(label.Substring(prefix.Length), out int chapterId) && chapterId > 0;
        }

        private static AddressableAssetGroup GetOrCreateChapterGroup(AddressableAssetSettings settings, int chapterId)
        {
            string groupName = $"Chapter_{chapterId}";
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group != null)
            {
                if (group.GetSchema<BundledAssetGroupSchema>() == null || group.GetSchema<ContentUpdateGroupSchema>() == null)
                    throw new InvalidOperationException($"Existing Addressables group '{groupName}' does not have the required bundled-content schemas.");
                return group;
            }

            group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            BundledAssetGroupSchema bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            group.GetSchema<ContentUpdateGroupSchema>().StaticContent = false;
            return group;
        }

        private static AddressableAssetGroup GetOrCreateChapterPreviewGroup(AddressableAssetSettings settings, int chapterId)
        {
            string groupName = $"Chapter_{chapterId}_Preview";
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            BundledAssetGroupSchema bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            ContentUpdateGroupSchema contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
            if (bundledSchema == null || contentUpdateSchema == null)
                throw new InvalidOperationException($"Existing Addressables group '{groupName}' does not have the required bundled-content schemas.");

            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            contentUpdateSchema.StaticContent = false;
            return group;
        }

        private static AddressableAssetGroup GetOrCreateChapterUiGroup(AddressableAssetSettings settings, int chapterId)
        {
            string groupName = $"Chapter_{chapterId}_UI";
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            BundledAssetGroupSchema bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            ContentUpdateGroupSchema contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
            if (bundledSchema == null || contentUpdateSchema == null)
                throw new InvalidOperationException($"Existing Addressables group '{groupName}' does not have the required bundled-content schemas.");

            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            contentUpdateSchema.StaticContent = false;
            return group;
        }

        private static AddressableAssetEntry FindEntryByAddress(AddressableAssetSettings settings, string address)
        {
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null) continue;
                foreach (AddressableAssetEntry entry in group.entries)
                    if (entry.address == address) return entry;
            }
            return null;
        }
    }
}
#endif
