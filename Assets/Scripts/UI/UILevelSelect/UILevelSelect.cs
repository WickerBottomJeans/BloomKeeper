using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UILevelSelect : MonoBehaviour
    {
        [SerializeField] private LevelMapBackgroundLayer backgroundLayer;
        [SerializeField] private LevelMapButtonLayer mapButtonLayer;

        private int? displayedChapterId;

        public event Action<int> OnLevelSelected;

        private void Awake()
        {
            mapButtonLayer.OnLevelSelected += HandleLevelSelected;
        }

        public async UniTask Show(ChapterContent chapterContent, PlayerProgressionData progression)
        {
            if (chapterContent == null) throw new ArgumentNullException(nameof(chapterContent));
            if (progression == null) throw new ArgumentNullException(nameof(progression));
            ChapterDefinition chapter = chapterContent.Definition;

            gameObject.SetActive(true);
            if (displayedChapterId == chapter.chapterId)
            {
                mapButtonLayer.Refresh(progression);
                return;
            }

            await backgroundLayer.ShowChapterAsync(chapter.backgroundChunks);
            await mapButtonLayer.ShowChapterAsync(chapterContent, chapter.backgroundChunks[0].width, progression);
            displayedChapterId = chapter.chapterId;
        }

        public UniTask WaitForInitialBackgroundLoaded()
        {
            return backgroundLayer.WaitForInitialChunksLoaded();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleLevelSelected(int levelId)
        {
            OnLevelSelected?.Invoke(levelId);
        }

        private void OnDestroy()
        {
            mapButtonLayer.OnLevelSelected -= HandleLevelSelected;
        }
    }
}
