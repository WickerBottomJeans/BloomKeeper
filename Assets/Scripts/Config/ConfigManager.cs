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
        private readonly ShopCachePolicyLoader shopCachePolicyLoader;
        private readonly Dictionary<int, ChapterDefinition> chapterDefinitions = new();
        private readonly Dictionary<int, LevelData> levelDefinitions = new();
        private ChapterIndex chapterIndex;
        private ShopCachePolicyConfig mainShopCachePolicy;

        public ChapterIndex ChapterIndex => chapterIndex ?? throw new InvalidOperationException("ConfigManager has not loaded the chapter index.");
        public ShopCachePolicyConfig MainShopCachePolicy => mainShopCachePolicy ?? throw new InvalidOperationException("ConfigManager has not loaded the main shop cache policy.");

        private ConfigManager()
        {
            var remoteJsonLoader = new RemoteJsonLoader(RemoteConfigBaseUrl);
            chapterIndexLoader = new ChapterIndexLoader(remoteJsonLoader);
            chapterDefinitionLoader = new ChapterDefinitionLoader(remoteJsonLoader);
            levelDataLoader = new LevelDataLoader(remoteJsonLoader);
            shopCachePolicyLoader = new ShopCachePolicyLoader(remoteJsonLoader);
        }

        public async UniTask InitializeAsync()
        {
            UniTask<ChapterIndex> chapterIndexTask = chapterIndexLoader.LoadAsync();
            UniTask<ShopCachePolicyConfig> mainShopCachePolicyTask = shopCachePolicyLoader.LoadMainShopCachePolicyAsync();
            chapterIndex = await chapterIndexTask;
            mainShopCachePolicy = await mainShopCachePolicyTask;
            ValidateShopCachePolicy(mainShopCachePolicy);
        }

        public async UniTask<ChapterDefinition> GetChapterDefinitionAsync(int chapterId)
        {
            if (chapterDefinitions.TryGetValue(chapterId, out ChapterDefinition chapterDefinition))
                return chapterDefinition;

            ChapterIndexEntry chapterEntry = ChapterIndex.GetEntry(chapterId);
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

        private static void ValidateShopCachePolicy(ShopCachePolicyConfig shopCachePolicy)
        {
            if (shopCachePolicy == null) throw new InvalidOperationException("Main shop cache policy contains invalid JSON.");
            if (shopCachePolicy.schemaVersion != ShopCachePolicyConfig.CurrentSchemaVersion) throw new InvalidOperationException($"Main shop cache policy schema version {shopCachePolicy.schemaVersion} is unsupported. Expected {ShopCachePolicyConfig.CurrentSchemaVersion}.");
            if (shopCachePolicy.revision <= 0) throw new InvalidOperationException("Main shop cache policy revision must be greater than zero.");
            if (shopCachePolicy.cacheLifetimeSeconds <= 0) throw new InvalidOperationException("Main shop cache policy lifetime must be greater than zero.");
        }

    }
}
