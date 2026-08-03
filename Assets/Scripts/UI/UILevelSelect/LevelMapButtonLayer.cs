using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class LevelMapButtonLayer : MonoBehaviour
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
        private float halfHeightBtn;

        public event Action<int> OnLevelSelected;

        public async UniTask ShowChapterAsync(ChapterContent chapterContent, float mapPixelWidth, PlayerProgressionData progression)
        {
            if (chapterContent == null) throw new ArgumentNullException(nameof(chapterContent));
            IReadOnlyList<ChapterLevelDisplayData> levels = chapterContent.Definition.levels;
            if (levels.Count == 0) throw new ArgumentException("A chapter must contain at least one level.", nameof(levels));
            if (mapPixelWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(mapPixelWidth), mapPixelWidth, "Map pixel width must be positive.");

            DisposeChapter();
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(chapterContent.Definition.levelButtonPrefabAddress);
            try
            {
                GameObject prefabObject = await loadHandle.ToUniTask();
                LevelButton levelButtonPrefab = prefabObject.GetComponent<LevelButton>();
                if (levelButtonPrefab == null)
                    throw new InvalidOperationException($"Addressable prefab '{chapterContent.Definition.levelButtonPrefabAddress}' does not contain a LevelButton component on its root.");

                levelButtonPrefabHandle = loadHandle;
                halfHeightBtn = levelButtonPrefab.GetComponent<RectTransform>().rect.height / 2f;
                scrollPool = new VerticalScrollPool<LevelButton>(content, viewport, scrollRect, levelButtonPrefab, levels.Count, i => new Vector2(levels[i].pixelX * (content.rect.width / mapPixelWidth), levels[i].pixelY * (content.rect.width / mapPixelWidth)), i => halfHeightBtn, null, (button, i) => button.Init(levels[i], GetEarnedStars(levels[i].levelId), chapterContent.GetLevelDefinition(levels[i].levelId).StarCap, IsLevelUnlocked(levels[i].levelId), HandleLevelSelected), button => { }, defaultPoolCapacity, maxPoolSize);
            }
            catch
            {
                if (loadHandle.IsValid()) Addressables.Release(loadHandle);
                levelButtonPrefabHandle = default;
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
        }
    }
}
