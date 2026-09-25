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
        private static ConfigManager instance;

        public static ConfigManager Instance => instance ?? throw new InvalidOperationException("ConfigManager has not been initialized with an environment config.");

        private readonly ChapterIndexLoader chapterIndexLoader;
        private readonly ChapterDefinitionLoader chapterDefinitionLoader;
        private readonly LevelDataLoader levelDataLoader;
        private readonly ShopCachePolicyLoader shopCachePolicyLoader;
        private readonly ScoreLoader scoreLoader;
        private readonly Dictionary<int, ChapterDefinition> chapterDefinitions = new();
        private readonly Dictionary<int, LevelData> levelDefinitions = new();
        private ChapterIndex chapterIndex;
        private ShopCachePolicyConfig mainShopCachePolicy;
        private ScoreConfig scoreConfig;

        public ChapterIndex ChapterIndex => chapterIndex ?? throw new InvalidOperationException("ConfigManager has not loaded the chapter index.");
        public ShopCachePolicyConfig MainShopCachePolicy => mainShopCachePolicy ?? throw new InvalidOperationException("ConfigManager has not loaded the main shop cache policy.");
        public ScoreConfig ScoreConfig => scoreConfig ?? throw new InvalidOperationException("ConfigManager has not loaded the score config.");

        private ConfigManager(GameEnvironmentConfig gameEnvironmentConfig)
        {
            var remoteJsonLoader = new RemoteJsonLoader(gameEnvironmentConfig.ConfigBaseUrl);
            chapterIndexLoader = new ChapterIndexLoader(remoteJsonLoader);
            chapterDefinitionLoader = new ChapterDefinitionLoader(remoteJsonLoader);
            levelDataLoader = new LevelDataLoader(remoteJsonLoader);
            shopCachePolicyLoader = new ShopCachePolicyLoader(remoteJsonLoader);
            scoreLoader = new ScoreLoader(remoteJsonLoader);
        }

        /// <summary>
        /// Creates the session's config loaders without downloading content.
        /// </summary>
        public static void InitializeConfigManager(GameEnvironmentConfig gameEnvironmentConfig)
        {
            if (instance != null) throw new InvalidOperationException("ConfigManager is already initialized for this session.");
            gameEnvironmentConfig.ValidateEnvironmentConfig();
            instance = new ConfigManager(gameEnvironmentConfig);
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetConfigManager()
        {
            instance = null;
        }

        public async UniTask InitializeAsync()
        {
            UniTask<ChapterIndex> chapterIndexTask = chapterIndexLoader.LoadAsync();
            UniTask<ShopCachePolicyConfig> mainShopCachePolicyTask = shopCachePolicyLoader.LoadMainShopCachePolicyAsync();
            UniTask<ScoreConfigJson> scoreConfigTask = scoreLoader.LoadScoreConfigAsync();
            var loadedConfigs = await UniTask.WhenAll(chapterIndexTask, mainShopCachePolicyTask, scoreConfigTask);
            chapterIndex = loadedConfigs.Item1;
            mainShopCachePolicy = loadedConfigs.Item2;
            ValidateShopCachePolicy(mainShopCachePolicy);
            scoreConfig = new ScoreConfig(loadedConfigs.Item3);
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
