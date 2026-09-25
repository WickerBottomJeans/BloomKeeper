#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DefaultNamespace.Editor
{
    /// <summary>
    /// Authors level JSON without creating gameplay objects or starting level attempts.
    /// </summary>
    public class LevelDefinitionEditor : EditorWindow
    {
        private const string LevelDirectoryPath = "ServerData/configs/levels";
        [SerializeField] private string draftJson;
        [SerializeField] private string savedJson;
        [SerializeField] private string sourcePath;
        [SerializeField] private string sourceFileContents;
        [SerializeField] private int requestedWidth = 8;
        [SerializeField] private int requestedHeight = 8;
        [SerializeField] private bool brushIsVoid;
        [SerializeField] private TileType brushTileType = TileType.Normal;
        [SerializeField] private int brushWebLevel = 1;
        [SerializeField] private PetalType brushPetalType;
        [SerializeField] private SpecialSkillType brushSkillType;
        [SerializeField] private float cellSize = 86f;
        private Vector2 boardScroll;
        private Vector2 settingsScroll;
        private Vector2 libraryScroll;
        private string levelSearch = "";
        private string[] levelPaths = Array.Empty<string>();
        private List<string> validationErrors = new List<string>();
        private string operationError;
        private int? previousPaintIndex;
        private int? paintUndoGroup;
        private GUIStyle cellLabelStyle;

        #region Unity Lifecycle
        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleLevelUndoRedo;
            saveChangesMessage = "Save changes to this level before closing?";
            if (string.IsNullOrEmpty(draftJson)) CreateNewLevelDocument();
            RefreshLevelLibrary();
            RefreshLevelValidation();
        }

        private void OnDisable()
        {
            FinishPaintStroke();
            Undo.undoRedoPerformed -= HandleLevelUndoRedo;
        }

        private void OnGUI()
        {
            DrawDocumentToolbar();
            LevelData levelData = JsonConvert.DeserializeObject<LevelData>(draftJson);
            if (!string.IsNullOrEmpty(operationError)) EditorGUILayout.HelpBox(operationError, MessageType.Error);
            EditorGUILayout.BeginHorizontal();
            DrawLevelLibrary();
            DrawBoardWorkspace(levelData);
            DrawLevelSettings(levelData);
            EditorGUILayout.EndHorizontal();
            CommitLevelEdit(levelData);
            if (Event.current.rawType == EventType.MouseUp) FinishPaintStroke();
        }
        #endregion

        #region Public API
        [MenuItem("Tools/Level Definition Editor")]
        public static void ShowLevelEditor()
        {
            LevelDefinitionEditor window = GetWindow<LevelDefinitionEditor>("Level Definition Editor");
            window.minSize = new Vector2(1050f, 650f);
        }

        public override void SaveChanges()
        {
            if (SaveLevelDocument()) base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            draftJson = savedJson;
            base.DiscardChanges();
        }
        #endregion

        #region Private Methods
        private void DrawDocumentToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("New", EditorStyles.toolbarButton) && ConfirmDocumentReplacement())
            {
                CreateNewLevelDocument();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Open…", EditorStyles.toolbarButton))
            {
                string path = EditorUtility.OpenFilePanel("Open level JSON", Path.GetFullPath(LevelDirectoryPath), "json");
                if (!string.IsNullOrEmpty(path)) OpenLevelDocument(path);
            }
            if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton))
            {
                string duplicateJson = draftJson;
                if (ConfirmDocumentReplacement())
                {
                    ReplaceLevelDocument(duplicateJson, null, null, false);
                    GUIUtility.ExitGUI();
                }
            }
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) SaveLevelDocument();
            if (GUILayout.Button("Undo", EditorStyles.toolbarButton))
            {
                Undo.PerformUndo();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Redo", EditorStyles.toolbarButton))
            {
                Undo.PerformRedo();
                GUIUtility.ExitGUI();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{(string.IsNullOrEmpty(sourcePath) ? "Unsaved level" : Path.GetFileName(sourcePath))}{(hasUnsavedChanges ? " *" : "")}");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelLibrary()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(180f));
            GUILayout.Label("LEVEL LIBRARY", EditorStyles.boldLabel);
            levelSearch = EditorGUILayout.TextField(levelSearch, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Refresh files")) RefreshLevelLibrary();
            libraryScroll = EditorGUILayout.BeginScrollView(libraryScroll);
            foreach (string levelPath in levelPaths)
            {
                string filename = Path.GetFileNameWithoutExtension(levelPath);
                if (filename.IndexOf(levelSearch, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Button(filename)) OpenLevelDocument(levelPath);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("Files are saved locally. Uploading configs and placing chapter map buttons are separate operations.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawBoardWorkspace(LevelData levelData)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("BOARD", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 55f;
            requestedWidth = EditorGUILayout.IntField("Columns", requestedWidth);
            requestedHeight = EditorGUILayout.IntField("Rows", requestedHeight);
            if (GUILayout.Button("Resize")) ResizeLevelBoard(levelData);
            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUILayout.EndHorizontal();
            cellSize = EditorGUILayout.Slider("Zoom", cellSize, 48f, 120f);
            EditorGUILayout.HelpBox("Drag: paint whole cells • Right click: pick brush • Rows run top to bottom\nRANDOM means the game chooses the flower. This is a design view, not a simulated board.", MessageType.None);

            boardScroll = EditorGUILayout.BeginScrollView(boardScroll, GUILayout.ExpandHeight(true));
            Rect boardRect = GUILayoutUtility.GetRect(levelData.boardWidth * cellSize, levelData.boardHeight * cellSize, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            cellLabelStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = 10 };
            cellLabelStyle.normal.textColor = Color.white;
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(boardScroll.y / cellSize) - 1);
            int lastRow = Mathf.Min(levelData.boardHeight, firstRow + Mathf.CeilToInt(position.height / cellSize) + 2);
            int firstColumn = Mathf.Max(0, Mathf.FloorToInt(boardScroll.x / cellSize) - 1);
            int lastColumn = Mathf.Min(levelData.boardWidth, firstColumn + Mathf.CeilToInt(position.width / cellSize) + 2);
            for (int row = firstRow; row < lastRow; row++)
            {
                for (int column = firstColumn; column < lastColumn; column++)
                {
                    int index = row * levelData.boardWidth + column;
                    TileData tileData = levelData.tiles[index];
                    Rect cellRect = new Rect(boardRect.x + column * cellSize, boardRect.y + row * cellSize, cellSize - 2f, cellSize - 2f);
                    EditorGUI.DrawRect(cellRect, GetTileColor(tileData));
                    string label = GetTileLabel(tileData);
                    GUI.Label(cellRect, new GUIContent(label, $"Column {column + 1}, row {row + 1} from top\n{label}\nSkill: {tileData.skillType}"), cellLabelStyle);
                }
            }
            HandleBoardPainting(levelData, boardRect);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLevelSettings(LevelData levelData)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(330f));
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 140f;
            DrawTileBrush(levelData);
            GUILayout.Space(12f);
            GUILayout.Label("LEVEL SETTINGS", EditorStyles.boldLabel);
            levelData.levelId = EditorGUILayout.IntField("Level ID", levelData.levelId);
            levelData.chapterId = EditorGUILayout.IntField("Chapter ID", levelData.chapterId);
            levelData.published = EditorGUILayout.Toggle("Published config flag", levelData.published);
            bool hasNextLevel = EditorGUILayout.Toggle("Has next level", levelData.nextLevelId.HasValue);
            if (hasNextLevel) levelData.nextLevelId = EditorGUILayout.IntField("Next level ID", levelData.nextLevelId ?? levelData.levelId);
            else levelData.nextLevelId = null;
            GUILayout.Label("Allowed boosters", EditorStyles.boldLabel);
            foreach (BoosterType boosterType in Enum.GetValues(typeof(BoosterType)))
            {
                bool allowed = levelData.allowedBoosters.Contains(boosterType);
                bool updated = EditorGUILayout.Toggle(boosterType.ToString(), allowed);
                if (updated && !allowed) levelData.allowedBoosters.Add(boosterType);
                if (!updated && allowed) levelData.allowedBoosters.Remove(boosterType);
            }
            DrawLevelObjectives(levelData);
            DrawLevelConstraints(levelData);
            DrawStarThresholds(levelData);
            GUILayout.Space(12f);
            GUILayout.Label("VALIDATION", EditorStyles.boldLabel);
            if (validationErrors.Count == 0) EditorGUILayout.HelpBox("Data checks passed. This does not prove the level is solvable or balanced.", MessageType.Info);
            foreach (string error in validationErrors) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (levelData.constrainers.Count == 0) EditorGUILayout.HelpBox("No constraints: this level has no move or time limit.", MessageType.Warning);
            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTileBrush(LevelData levelData)
        {
            GUILayout.Label("TILE BRUSH", EditorStyles.boldLabel);
            brushIsVoid = EditorGUILayout.Toggle("Empty board space", brushIsVoid);
            using (new EditorGUI.DisabledScope(brushIsVoid))
            {
                brushTileType = (TileType)EditorGUILayout.EnumPopup("Tile", brushTileType);
                if (brushTileType == TileType.Web) brushWebLevel = EditorGUILayout.IntField("Web layers", brushWebLevel);
                bool canContainPetal = brushTileType == TileType.Normal || brushTileType == TileType.Web && brushWebLevel == 0;
                using (new EditorGUI.DisabledScope(!canContainPetal))
                {
                    brushPetalType = (PetalType)EditorGUILayout.EnumPopup("Flower (None = random)", brushPetalType);
                    using (new EditorGUI.DisabledScope(brushPetalType == PetalType.None)) brushSkillType = (SpecialSkillType)EditorGUILayout.EnumPopup("Skill", brushSkillType);
                }
            }
            if (GUILayout.Button("Fill board with brush") && EditorUtility.DisplayDialog("Fill board", "Replace every cell with the current brush? You can undo this action.", "Fill", "Cancel"))
                for (int index = 0; index < levelData.tiles.Count; index++) levelData.tiles[index] = CreateBrushTileData();
        }

        private void DrawLevelObjectives(LevelData levelData)
        {
            GUILayout.Space(12f);
            GUILayout.Label("OBJECTIVES", EditorStyles.boldLabel);
            for (int index = 0; index < levelData.objectives.Count; index++)
            {
                ObjectiveJson objectiveJson = levelData.objectives[index];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(objectiveJson.type.ToString(), EditorStyles.boldLabel);
                bool remove = GUILayout.Button("Remove", GUILayout.Width(65f));
                EditorGUILayout.EndHorizontal();
                if (objectiveJson.type == ObjectiveType.Match)
                {
                    for (int goalIndex = 0; goalIndex < objectiveJson.petals.Count; goalIndex++)
                    {
                        PetalGoal petalGoal = objectiveJson.petals[goalIndex];
                        EditorGUILayout.BeginHorizontal();
                        petalGoal.petalType = (PetalType)EditorGUILayout.EnumPopup(petalGoal.petalType);
                        petalGoal.amount = EditorGUILayout.IntField(petalGoal.amount, GUILayout.Width(55f));
                        bool removeGoal = GUILayout.Button("×", GUILayout.Width(24f));
                        EditorGUILayout.EndHorizontal();
                        if (removeGoal) objectiveJson.petals.RemoveAt(goalIndex--);
                    }
                    if (GUILayout.Button("Add flower goal")) objectiveJson.petals.Add(new PetalGoal { petalType = PetalType.Strawberry, amount = 10 });
                }
                if (objectiveJson.type == ObjectiveType.ClearSpiderWeb) objectiveJson.spiderWebsToClear = EditorGUILayout.IntField("Web tiles to clear", objectiveJson.spiderWebsToClear);
                EditorGUILayout.EndVertical();
                if (remove) levelData.objectives.RemoveAt(index--);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Match")) levelData.objectives.Add(new ObjectiveJson { type = ObjectiveType.Match, petals = new List<PetalGoal> { new PetalGoal { petalType = PetalType.Strawberry, amount = 10 } } });
            if (GUILayout.Button("+ Clear webs")) levelData.objectives.Add(new ObjectiveJson { type = ObjectiveType.ClearSpiderWeb, spiderWebsToClear = 1 });
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelConstraints(LevelData levelData)
        {
            GUILayout.Space(12f);
            GUILayout.Label("LIMITS", EditorStyles.boldLabel);
            for (int index = 0; index < levelData.constrainers.Count; index++)
            {
                ConstrainerJson constrainerJson = levelData.constrainers[index];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(constrainerJson.type.ToString(), EditorStyles.boldLabel);
                bool remove = GUILayout.Button("Remove", GUILayout.Width(65f));
                EditorGUILayout.EndHorizontal();
                if (constrainerJson.type == ConstrainerType.MoveLimit) constrainerJson.moveLimit = EditorGUILayout.IntField("Moves", constrainerJson.moveLimit);
                else constrainerJson.timeLimitSeconds = EditorGUILayout.FloatField("Seconds", constrainerJson.timeLimitSeconds);
                constrainerJson.warningAtRemaining = EditorGUILayout.IntField("Warn at remaining", constrainerJson.warningAtRemaining);
                EditorGUILayout.EndVertical();
                if (remove) levelData.constrainers.RemoveAt(index--);
            }
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(levelData.constrainers.Any(value => value.type == ConstrainerType.MoveLimit)))
                if (GUILayout.Button("+ Move limit")) levelData.constrainers.Add(new ConstrainerJson { type = ConstrainerType.MoveLimit, moveLimit = 20, warningAtRemaining = 5 });
            using (new EditorGUI.DisabledScope(levelData.constrainers.Any(value => value.type == ConstrainerType.TimeLimit)))
                if (GUILayout.Button("+ Time limit")) levelData.constrainers.Add(new ConstrainerJson { type = ConstrainerType.TimeLimit, timeLimitSeconds = 60f, warningAtRemaining = 10 });
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStarThresholds(LevelData levelData)
        {
            GUILayout.Space(12f);
            GUILayout.Label("STAR THRESHOLDS — STARS / SCORE", EditorStyles.boldLabel);
            for (int index = 0; index < levelData.starScoreThresholds.Count; index++)
            {
                StarScoreThresholdJson thresholdJson = levelData.starScoreThresholds[index];
                EditorGUILayout.BeginHorizontal();
                thresholdJson.starCount = EditorGUILayout.IntField(thresholdJson.starCount, GUILayout.Width(55f));
                thresholdJson.score = EditorGUILayout.IntField(thresholdJson.score);
                bool remove = GUILayout.Button("×", GUILayout.Width(24f));
                EditorGUILayout.EndHorizontal();
                if (remove) levelData.starScoreThresholds.RemoveAt(index--);
            }
            if (GUILayout.Button("Add star threshold")) levelData.starScoreThresholds.Add(new StarScoreThresholdJson());
        }

        private void HandleBoardPainting(LevelData levelData, Rect boardRect)
        {
            Event currentEvent = Event.current;
            if (!boardRect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.MouseDrag) previousPaintIndex = null;
                return;
            }
            int column = Mathf.FloorToInt((currentEvent.mousePosition.x - boardRect.x) / cellSize);
            int row = Mathf.FloorToInt((currentEvent.mousePosition.y - boardRect.y) / cellSize);
            int index = row * levelData.boardWidth + column;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
            {
                TileData tileData = levelData.tiles[index];
                brushIsVoid = tileData.isVoid;
                brushTileType = tileData.type;
                brushWebLevel = tileData.webLevel;
                brushPetalType = tileData.petalType;
                brushSkillType = tileData.skillType;
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.button == 0 && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag && paintUndoGroup.HasValue))
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    Undo.IncrementCurrentGroup();
                    paintUndoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Paint level board");
                }
                int startColumn = previousPaintIndex.HasValue ? previousPaintIndex.Value % levelData.boardWidth : column;
                int startRow = previousPaintIndex.HasValue ? previousPaintIndex.Value / levelData.boardWidth : row;
                int steps = Math.Max(Math.Abs(column - startColumn), Math.Abs(row - startRow));
                for (int step = 0; step <= steps; step++)
                {
                    float fraction = steps == 0 ? 0f : (float)step / steps;
                    int paintColumn = Mathf.RoundToInt(Mathf.Lerp(startColumn, column, fraction));
                    int paintRow = Mathf.RoundToInt(Mathf.Lerp(startRow, row, fraction));
                    levelData.tiles[paintRow * levelData.boardWidth + paintColumn] = CreateBrushTileData();
                }
                previousPaintIndex = index;
                currentEvent.Use();
                Repaint();
            }
        }

        private TileData CreateBrushTileData()
        {
            bool canContainPetal = !brushIsVoid && (brushTileType == TileType.Normal || brushTileType == TileType.Web && brushWebLevel == 0);
            return new TileData { isVoid = brushIsVoid, type = brushTileType, webLevel = !brushIsVoid && brushTileType == TileType.Web ? brushWebLevel : 0, petalType = canContainPetal ? brushPetalType : PetalType.None, skillType = canContainPetal && brushPetalType != PetalType.None ? brushSkillType : SpecialSkillType.None };
        }

        private void ResizeLevelBoard(LevelData levelData)
        {
            if (requestedWidth <= 0 || requestedHeight <= 0 || (long)requestedWidth * requestedHeight > int.MaxValue)
            {
                operationError = "Enter positive board dimensions whose tile count fits in an integer.";
                return;
            }
            if (requestedWidth == levelData.boardWidth && requestedHeight == levelData.boardHeight) return;
            if ((requestedWidth < levelData.boardWidth || requestedHeight < levelData.boardHeight) && !EditorUtility.DisplayDialog("Shrink board", "Cells outside the new size will be removed. The top left corner stays in place. You can undo this action.", "Resize", "Cancel")) return;
            var tiles = new List<TileData>(requestedWidth * requestedHeight);
            for (int row = 0; row < requestedHeight; row++)
                for (int column = 0; column < requestedWidth; column++)
                    tiles.Add(row < levelData.boardHeight && column < levelData.boardWidth ? levelData.tiles[row * levelData.boardWidth + column] : new TileData { type = TileType.Normal });
            levelData.tiles = tiles;
            levelData.boardWidth = requestedWidth;
            levelData.boardHeight = requestedHeight;
            operationError = null;
        }

        private void CreateNewLevelDocument()
        {
            var levelData = new LevelData { levelId = 1, chapterId = 1, boardWidth = 8, boardHeight = 8, allowedBoosters = new List<BoosterType>(), tiles = new List<TileData>(), objectives = new List<ObjectiveJson> { new ObjectiveJson { type = ObjectiveType.Match, petals = new List<PetalGoal> { new PetalGoal { petalType = PetalType.Strawberry, amount = 10 } } } }, constrainers = new List<ConstrainerJson> { new ConstrainerJson { type = ConstrainerType.MoveLimit, moveLimit = 20, warningAtRemaining = 5 } }, starScoreThresholds = new List<StarScoreThresholdJson> { new StarScoreThresholdJson { starCount = 1, score = 100 }, new StarScoreThresholdJson { starCount = 2, score = 300 }, new StarScoreThresholdJson { starCount = 3, score = 1000 } } };
            for (int index = 0; index < levelData.boardWidth * levelData.boardHeight; index++) levelData.tiles.Add(new TileData { type = TileType.Normal });
            ReplaceLevelDocument(JsonConvert.SerializeObject(levelData, Formatting.Indented), null, null, false);
        }

        private void ReplaceLevelDocument(string json, string path, string fileContents, bool isSaved)
        {
            FinishPaintStroke();
            Undo.ClearUndo(this);
            draftJson = json;
            savedJson = isSaved ? json : null;
            sourcePath = path;
            sourceFileContents = fileContents;
            LevelData levelData = JsonConvert.DeserializeObject<LevelData>(json);
            requestedWidth = levelData.boardWidth;
            requestedHeight = levelData.boardHeight;
            operationError = null;
            boardScroll = Vector2.zero;
            RefreshLevelValidation();
        }

        private void OpenLevelDocument(string path)
        {
            try
            {
                string fileContents = File.ReadAllText(path);
                JObject document = JObject.Parse(fileContents, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                LevelData levelData = document.ToObject<LevelData>(JsonSerializer.Create(new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error }));
                List<string> errors = LevelDefinitionValidator.ValidateLevelData(levelData);
                if (errors.Count > 0) throw new InvalidDataException("Cannot open this level until its data errors are fixed:\n" + string.Join("\n", errors));
                if (!ConfirmDocumentReplacement()) return;
                ReplaceLevelDocument(JsonConvert.SerializeObject(levelData, Formatting.Indented), Path.GetFullPath(path), fileContents, true);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
            {
                operationError = exception.Message;
                return;
            }
            GUIUtility.ExitGUI();
        }

        private bool SaveLevelDocument()
        {
            FinishPaintStroke();
            RefreshLevelValidation();
            if (validationErrors.Count > 0)
            {
                operationError = "Save blocked. Fix the errors in the Validation section.";
                Repaint();
                return false;
            }
            LevelData levelData = JsonConvert.DeserializeObject<LevelData>(draftJson);
            string destinationPath = Path.GetFullPath(Path.Combine(LevelDirectoryPath, $"level_{levelData.levelId}.json"));
            string temporaryPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                bool destinationExists = File.Exists(destinationPath);
                bool sameSource = string.Equals(destinationPath, sourcePath, StringComparison.OrdinalIgnoreCase);
                if (destinationExists && (!sameSource || File.ReadAllText(destinationPath) != sourceFileContents))
                {
                    if (!EditorUtility.DisplayDialog("Overwrite level JSON", $"{destinationPath}\n\nThis file already exists or changed outside the editor. Replace it with this document?", "Replace", "Cancel")) return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.WriteAllText(temporaryPath, draftJson + Environment.NewLine);
                if (destinationExists) File.Replace(temporaryPath, destinationPath, null);
                else File.Move(temporaryPath, destinationPath);
                savedJson = draftJson;
                sourceFileContents = draftJson + Environment.NewLine;
                sourcePath = destinationPath;
                hasUnsavedChanges = false;
                operationError = null;
                RefreshLevelLibrary();
                ShowNotification(new GUIContent("Level JSON saved locally"));
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                operationError = $"Save failed: {exception.Message}";
                Repaint();
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); }
                    catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) { operationError = $"Temporary file cleanup failed: {exception.Message}"; }
                }
            }
        }

        private bool ConfirmDocumentReplacement()
        {
            if (!hasUnsavedChanges) return true;
            int choice = EditorUtility.DisplayDialogComplex("Unsaved level", "Save changes before switching documents?", "Save", "Cancel", "Discard");
            return choice == 2 || choice == 0 && SaveLevelDocument();
        }

        private void CommitLevelEdit(LevelData levelData)
        {
            string updatedJson = JsonConvert.SerializeObject(levelData, Formatting.Indented);
            if (updatedJson == draftJson) return;
            Undo.RecordObject(this, paintUndoGroup.HasValue ? "Paint level board" : "Edit level definition");
            draftJson = updatedJson;
            EditorUtility.SetDirty(this);
            RefreshLevelValidation();
            Repaint();
        }

        private void RefreshLevelValidation()
        {
            validationErrors = LevelDefinitionValidator.ValidateLevelData(JsonConvert.DeserializeObject<LevelData>(draftJson));
            hasUnsavedChanges = draftJson != savedJson;
        }

        private void RefreshLevelLibrary()
        {
            try
            {
                levelPaths = Directory.Exists(LevelDirectoryPath) ? Directory.GetFiles(LevelDirectoryPath, "level_*.json").OrderBy(path => path, Comparer<string>.Create(EditorUtility.NaturalCompare)).ToArray() : Array.Empty<string>();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) { operationError = $"Cannot read the level library: {exception.Message}"; }
        }

        private void HandleLevelUndoRedo()
        {
            FinishPaintStroke();
            RefreshLevelValidation();
            LevelData levelData = JsonConvert.DeserializeObject<LevelData>(draftJson);
            requestedWidth = levelData.boardWidth;
            requestedHeight = levelData.boardHeight;
            Repaint();
        }

        private void FinishPaintStroke()
        {
            if (paintUndoGroup.HasValue)
            {
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(paintUndoGroup.Value);
            }
            paintUndoGroup = null;
            previousPaintIndex = null;
        }

        private static string GetTileLabel(TileData tileData)
        {
            if (tileData.isVoid) return "VOID";
            if (tileData.type == TileType.Inactive) return "INACTIVE";
            if (tileData.type == TileType.Web && tileData.webLevel > 0) return $"WEB\n{tileData.webLevel} layers";
            string flowerLabel = tileData.petalType == PetalType.None ? "RANDOM" : tileData.petalType.ToString();
            return tileData.skillType == SpecialSkillType.None ? flowerLabel : $"{flowerLabel}\n{tileData.skillType}";
        }

        private static Color GetTileColor(TileData tileData)
        {
            if (tileData.isVoid) return new Color(0.12f, 0.12f, 0.14f);
            if (tileData.type == TileType.Inactive) return new Color(0.28f, 0.28f, 0.3f);
            if (tileData.type == TileType.Web && tileData.webLevel > 0) return new Color(0.36f, 0.32f, 0.44f);
            if (tileData.petalType == PetalType.None) return new Color(0.2f, 0.37f, 0.34f);
            return Color.HSVToRGB((int)tileData.petalType / ((float)Enum.GetValues(typeof(PetalType)).Length - 1), 0.62f, 0.55f);
        }
        #endregion
    }
}
#endif
