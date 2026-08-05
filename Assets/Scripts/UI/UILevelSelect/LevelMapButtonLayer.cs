using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class LevelMapButtonLayer : MonoBehaviour, IScrollPoolGeometrySource
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;

        [Header("Pooling Settings")]
        [SerializeField] private int defaultPoolCapacity = 10;
        [SerializeField] private int maxPoolSize = 50;

        private VerticalScrollPool<LevelButton> scrollPool;
        private AsyncOperationHandle<GameObject> levelButtonPrefabHandle;
        private PlayerProgressionData progression;
        private IReadOnlyList<ChapterLevelDisplayData> levels;
        private float mapPixelWidth;
        private float halfHeightBtn;

        public event Action<int> OnLevelSelected;

        public async UniTask ShowChapterAsync(ChapterContent chapterContent, float mapPixelWidth, PlayerProgressionData progression)
        {
            if (chapterContent == null) throw new ArgumentNullException(nameof(chapterContent));
            IReadOnlyList<ChapterLevelDisplayData> chapterLevels = chapterContent.Definition.levels;
            if (chapterLevels.Count == 0) throw new ArgumentException("A chapter must contain at least one level.", nameof(chapterLevels));
            if (mapPixelWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(mapPixelWidth), mapPixelWidth, "Map pixel width must be positive.");

            DisposeChapter();
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            levels = chapterLevels;
            this.mapPixelWidth = mapPixelWidth;
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(chapterContent.Definition.levelButtonPrefabAddress);
            try
            {
                GameObject prefabObject = await loadHandle.ToUniTask();
                LevelButton levelButtonPrefab = prefabObject.GetComponent<LevelButton>();
                if (levelButtonPrefab == null)
                    throw new InvalidOperationException($"Addressable prefab '{chapterContent.Definition.levelButtonPrefabAddress}' does not contain a LevelButton component on its root.");

                levelButtonPrefabHandle = loadHandle;
                halfHeightBtn = levelButtonPrefab.GetComponent<RectTransform>().rect.height / 2f;
                scrollPool = new VerticalScrollPool<LevelButton>(content, viewport, scrollRect, levelButtonPrefab, this, null, (button, i) => button.Init(levels[i], GetEarnedStars(levels[i].levelId), chapterContent.GetLevelDefinition(levels[i].levelId).StarCap, IsLevelUnlocked(levels[i].levelId), HandleLevelSelected), button => { }, defaultPoolCapacity, maxPoolSize);
            }
            catch
            {
                if (loadHandle.IsValid()) Addressables.Release(loadHandle);
                levelButtonPrefabHandle = default;
                levels = null;
                this.mapPixelWidth = 0f;
                throw;
            }
        }

        public void Refresh(PlayerProgressionData progression)
        {
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            scrollPool.Refresh();
        }

        private void HandleLevelSelected(int levelId)
        {
            OnLevelSelected?.Invoke(levelId);
        }

        private int GetEarnedStars(int levelId)
        {
            return progression.levels.TryGetValue(levelId, out LevelProgressData levelProgress) ? levelProgress.bestStars : 0;
        }

        private bool IsLevelUnlocked(int levelId)
        {
            return levelId <= progression.highestUnlockedLevel;
        }

        int IScrollPoolGeometrySource.Count => levels.Count;

        ScrollPoolItemGeometry IScrollPoolGeometrySource.GetGeometry(int index)
        {
            ChapterLevelDisplayData level = levels[index];
            float scale = content.rect.width / mapPixelWidth;
            return new ScrollPoolItemGeometry(new Vector2(level.pixelX * scale, level.pixelY * scale), halfHeightBtn);
        }

        private void OnDestroy()
        {
            DisposeChapter();
        }

        private void DisposeChapter()
        {
            if (scrollPool != null)
            {
                scrollPool.Dispose();
                scrollPool = null;
            }
            if (levelButtonPrefabHandle.IsValid()) Addressables.Release(levelButtonPrefabHandle);
            levelButtonPrefabHandle = default;
            levels = null;
            mapPixelWidth = 0f;
        }
    }
}
