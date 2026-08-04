using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    /// <summary>
    /// loads JSON configs for runtime use.
    /// </summary>
    public class ConfigManager
    {
        private const string RemoteConfigBaseUrl = "https://pub-600516f978894a4ba6eeac168a341d46.r2.dev/configs/";

        public static ConfigManager Instance { get; } = new ConfigManager();

        private readonly ChapterIndexLoader chapterIndexLoader;
        private readonly ChapterDefinitionLoader chapterDefinitionLoader;
        private readonly LevelDataLoader levelDataLoader;
        private readonly Dictionary<int, ChapterDefinition> chapterDefinitions = new();
        private readonly Dictionary<int, LevelData> levelDefinitions = new();
        private ChapterIndex chapterIndex;

        public ChapterIndex ChapterIndex => chapterIndex ?? throw new InvalidOperationException("ConfigManager has not loaded the chapter index.");

        private ConfigManager()
        {
            var remoteJsonLoader = new RemoteJsonLoader(RemoteConfigBaseUrl);
            chapterIndexLoader = new ChapterIndexLoader(remoteJsonLoader);
            chapterDefinitionLoader = new ChapterDefinitionLoader(remoteJsonLoader);
            levelDataLoader = new LevelDataLoader(remoteJsonLoader);
        }

        public async UniTask InitializeAsync()
        {
            chapterIndex = await chapterIndexLoader.LoadAsync();
        }

        public async UniTask<ChapterDefinition> GetChapterDefinitionAsync(int chapterId)
        {
            if (chapterDefinitions.TryGetValue(chapterId, out ChapterDefinition chapterDefinition))
                return chapterDefinition;

            ChapterIndexEntry chapterEntry = GetChapterEntry(chapterId);
            chapterDefinition = await chapterDefinitionLoader.LoadAsync(chapterEntry.configPath);
            chapterDefinitions.Add(chapterId, chapterDefinition);
            return chapterDefinition;
        }

        public async UniTask<LevelData> GetLevelDataAsync(int levelId)
        {
            if (levelDefinitions.TryGetValue(levelId, out LevelData levelDefinition))
                return levelDefinition;

            levelDefinition = await levelDataLoader.LoadAsync(levelId);
            levelDefinitions.Add(levelId, levelDefinition);
            return levelDefinition;
        }

        public async UniTask<ChapterContent> GetChapterContentAsync(int chapterId)
        {
            ChapterDefinition chapterDefinition = await GetChapterDefinitionAsync(chapterId);
            UniTask<LevelData>[] levelLoadTasks = new UniTask<LevelData>[chapterDefinition.levels.Count];
            for (int i = 0; i < chapterDefinition.levels.Count; i++)
                levelLoadTasks[i] = GetLevelDataAsync(chapterDefinition.levels[i].levelId);

            LevelData[] loadedLevels = await UniTask.WhenAll(levelLoadTasks);
            var chapterLevels = new Dictionary<int, LevelData>(loadedLevels.Length);
            for (int i = 0; i < loadedLevels.Length; i++)
                chapterLevels.Add(chapterDefinition.levels[i].levelId, loadedLevels[i]);

            return new ChapterContent(chapterDefinition, chapterLevels);
        }

        public bool TryGetNextLevelId(int currentLevelId, out int nextLevelId)
        {
            if (!levelDefinitions.TryGetValue(currentLevelId, out LevelData currentLevel))
                throw new InvalidOperationException($"Level {currentLevelId} has not been loaded into the config cache.");

            if (!currentLevel.nextLevelId.HasValue)
            {
                nextLevelId = default;
                return false;
            }

            nextLevelId = currentLevel.nextLevelId.Value;
            return true;
        }

        private ChapterIndexEntry GetChapterEntry(int chapterId)
        {
            foreach (ChapterIndexEntry chapterEntry in ChapterIndex.chapters)
                if (chapterEntry.chapterId == chapterId)
                    return chapterEntry;

            throw new KeyNotFoundException($"Chapter index has no entry for chapter {chapterId}.");
        }
    }
}
