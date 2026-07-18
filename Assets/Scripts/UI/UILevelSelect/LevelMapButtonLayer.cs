using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class LevelMapButtonLayer : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private LevelButton levelButtonPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;

        [Header("Pooling Settings")]
        [SerializeField] private int defaultPoolCapacity = 10;
        [SerializeField] private int maxPoolSize = 50;

        private VerticalScrollPool<LevelButton> scrollPool;
        private LevelMetaCollection metaCollection;
        private List<LevelMeta> allMetas;
        private PlayerProgressionData progression;
        private float halfHeightBtn;

        public event Action<int> OnLevelSelected;

        public void Init(PlayerProgressionData progression)
        {
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            LoadMetas();
            halfHeightBtn = levelButtonPrefab.GetComponent<RectTransform>().rect.height / 2f;

            scrollPool = new VerticalScrollPool<LevelButton>(
                content,
                viewport,
                scrollRect,
                levelButtonPrefab,
                allMetas.Count,
                i => new Vector2(allMetas[i].pixelX * (content.rect.width / metaCollection.referenceScreenWidth), allMetas[i].pixelY * (content.rect.width / metaCollection.referenceScreenWidth)),
                i => halfHeightBtn,
                null,
                (button, i) => button.Init(allMetas[i], GetEarnedStars(allMetas[i].levelId), IsLevelUnlocked(allMetas[i].levelId), HandleLevelSelected),
                button => { },
                defaultPoolCapacity,
                maxPoolSize
            );
        }

        public void Refresh(PlayerProgressionData progression)
        {
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            scrollPool.Refresh();
        }

        private void LoadMetas()
        {
            metaCollection = LevelLoader.LoadLevelMetas();
            allMetas = metaCollection.levels;
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
            scrollPool.Dispose();
        }
    }
}
