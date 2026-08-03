using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DefaultNamespace
{
    public class LocalChapterContentProvider : IChapterContentProvider
    {
        private const int SupportedSchemaVersion = 1;

        public async UniTask<ChapterContent> LoadChapterAsync(int chapterId)
        {
            if (chapterId <= 0)
                throw new ArgumentOutOfRangeException(nameof(chapterId), chapterId, "Chapter identity must be positive.");

            string path = Path.Combine(Application.streamingAssetsPath, "chapters", $"chapter_{chapterId}.json");
            string uri = path.Contains("://") ? path : new Uri(path).AbsoluteUri;

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new IOException($"Unable to load chapter {chapterId} from '{uri}': {request.error}");

                ChapterDefinition chapter;
                try
                {
                    chapter = JsonConvert.DeserializeObject<ChapterDefinition>(request.downloadHandler.text);
                }
                catch (JsonException exception)
                {
                    throw new InvalidDataException($"Chapter {chapterId} contains invalid JSON.", exception);
                }

                ValidateChapter(chapter, chapterId);
                UniTask<LevelData>[] levelLoadTasks = chapter.levels.Select(level => LoadLevelAsync(level.levelId)).ToArray();
                LevelData[] levelDefinitions = await UniTask.WhenAll(levelLoadTasks);
                var levelDefinitionsById = new Dictionary<int, LevelData>(levelDefinitions.Length);
                foreach (LevelData levelDefinition in levelDefinitions)
                {
                    if (!levelDefinitionsById.TryAdd(levelDefinition.levelId, levelDefinition))
                        throw new InvalidDataException($"Chapter {chapterId} loaded level {levelDefinition.levelId} more than once.");
                }
                return new ChapterContent(chapter, levelDefinitionsById);
            }
        }

        private static async UniTask<LevelData> LoadLevelAsync(int levelId)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "levels", $"level_{levelId}.json");
            string uri = path.Contains("://") ? path : new Uri(path).AbsoluteUri;
            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                    throw new IOException($"Unable to load level {levelId} from '{uri}': {request.error}");

                LevelData levelDefinition;
                try
                {
                    levelDefinition = JsonConvert.DeserializeObject<LevelData>(request.downloadHandler.text);
                }
                catch (JsonException exception)
                {
                    throw new InvalidDataException($"Level {levelId} contains invalid JSON.", exception);
                }

                if (levelDefinition == null)
                    throw new InvalidDataException($"Level {levelId} did not contain a level definition.");
                if (levelDefinition.levelId != levelId)
                    throw new InvalidDataException($"Requested level {levelId}, but the loaded definition identifies itself as level {levelDefinition.levelId}.");
                _ = levelDefinition.StarCap;
                return levelDefinition;
            }
        }

        private static void ValidateChapter(ChapterDefinition chapter, int requestedChapterId)
        {
            if (chapter == null)
                throw new InvalidDataException($"Chapter {requestedChapterId} did not contain a chapter definition.");
            if (chapter.schemaVersion != SupportedSchemaVersion)
                throw new InvalidDataException($"Chapter {requestedChapterId} uses unsupported schema version {chapter.schemaVersion}.");
            if (chapter.chapterId != requestedChapterId)
                throw new InvalidDataException($"Requested chapter {requestedChapterId}, but the loaded definition identifies itself as chapter {chapter.chapterId}.");
            if (string.IsNullOrWhiteSpace(chapter.chapterName))
                throw new InvalidDataException($"Chapter {requestedChapterId} must have a display name.");
            if (string.IsNullOrWhiteSpace(chapter.downloadLabel))
                throw new InvalidDataException($"Chapter {requestedChapterId} must advertise an Addressables download label.");
            if (string.IsNullOrWhiteSpace(chapter.topperPrefabAddress))
                throw new InvalidDataException($"Chapter {requestedChapterId} must advertise a topper prefab address.");
            if (string.IsNullOrWhiteSpace(chapter.bottomNavigationPrefabAddress))
                throw new InvalidDataException($"Chapter {requestedChapterId} must advertise a bottom navigation prefab address.");
            if (string.IsNullOrWhiteSpace(chapter.levelButtonPrefabAddress))
                throw new InvalidDataException($"Chapter {requestedChapterId} must advertise a level button prefab address.");
            if (chapter.levels == null || chapter.levels.Count == 0)
                throw new InvalidDataException($"Chapter {requestedChapterId} must advertise at least one level.");
            if (chapter.backgroundChunks == null || chapter.backgroundChunks.Count == 0)
                throw new InvalidDataException($"Chapter {requestedChapterId} must advertise at least one background chunk.");

            var levelIds = new HashSet<int>();
            foreach (ChapterLevelDisplayData level in chapter.levels)
            {
                if (level == null || level.levelId <= 0)
                    throw new InvalidDataException($"Chapter {requestedChapterId} contains an invalid level identity.");
                if (!levelIds.Add(level.levelId))
                    throw new InvalidDataException($"Chapter {requestedChapterId} advertises level {level.levelId} more than once.");
            }

            foreach (ChapterBackgroundChunkData chunk in chapter.backgroundChunks)
                if (chunk == null || string.IsNullOrWhiteSpace(chunk.address) || string.IsNullOrWhiteSpace(chunk.previewAddress) || chunk.width <= 0 || chunk.height <= 0)
                    throw new InvalidDataException($"Chapter {requestedChapterId} contains an invalid background chunk description.");
        }
    }
}
