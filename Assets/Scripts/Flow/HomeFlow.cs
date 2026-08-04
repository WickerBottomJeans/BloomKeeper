using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeFlow
    {
        private readonly AddressableContentService addressableContentService;
        private HomeMiddleTab? activeMiddleTab;
        private int? currentChapterId;

        public event Action<int> StartLevelRequested;
        public event Action SettingsRequested;
        public event Action AddLifeRequested;
        public event Action AddCurrencyRequested;

        public HomeFlow(AddressableContentService addressableContentService)
        {
            this.addressableContentService = addressableContentService ?? throw new ArgumentNullException(nameof(addressableContentService));
        }

        public async UniTask Enter()
        {
            UIManager.Instance.LevelSelected += HandleLevelSelected;
            UIManager.Instance.HomeTabRequested += HandleHomeTabRequested;
            UIManager.Instance.ChapterVisitRequested += HandleChapterVisitRequested;
            UIManager.Instance.ChapterChooserCloseRequested += HandleChapterChooserCloseRequested;
            UIManager.Instance.SettingsRequested += HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested += HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested += HandleAddCurrencyRequested;

            PlayerProgressionData progression = PlayerAccountContext.Instance.GetCurrentProgression();
            ChapterIndexEntry chapterEntry = currentChapterId.HasValue
                ? GetChapterEntry(ConfigManager.Instance.ChapterIndex, currentChapterId.Value)
                : ResolveLatestUnlockedChapter(ConfigManager.Instance.ChapterIndex, progression.highestUnlockedLevel);
            await addressableContentService.EnsureDownloadedAsync(chapterEntry.downloadLabel);
            ChapterContent chapterContent = await ConfigManager.Instance.GetChapterContentAsync(chapterEntry.chapterId);
            await UIManager.Instance.ShowHome(chapterContent, progression);
            await ChangeTabAsync(HomeMiddleTab.Map);
            currentChapterId = chapterEntry.chapterId;
        }

        public void Exit()
        {
            UIManager.Instance.LevelSelected -= HandleLevelSelected;
            UIManager.Instance.HomeTabRequested -= HandleHomeTabRequested;
            UIManager.Instance.ChapterVisitRequested -= HandleChapterVisitRequested;
            UIManager.Instance.ChapterChooserCloseRequested -= HandleChapterChooserCloseRequested;
            UIManager.Instance.SettingsRequested -= HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested -= HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested -= HandleAddCurrencyRequested;
            UIManager.Instance.HideHome();
            UIManager.Instance.HideChapterChooser();
            activeMiddleTab = null;
        }

        public void SetCurrentChapter(int chapterId)
        {
            currentChapterId = chapterId;
        }

        private void HandleLevelSelected(int levelId)
        {
            StartLevelRequested?.Invoke(levelId);
        }

        private void HandleHomeTabRequested(HomeMiddleTab tab)
        {
            if (tab == HomeMiddleTab.Map && activeMiddleTab == HomeMiddleTab.Map)
            {
                ApplicationOperationRunner.Instance.Run(OpenChapterChooserAsync);
                return;
            }

            ApplicationOperationRunner.Instance.Run(() => ChangeTabAsync(tab));
        }

        private async UniTask OpenChapterChooserAsync()
        {
            if (!currentChapterId.HasValue) throw new InvalidOperationException("Cannot open the chapter chooser before HomeFlow has selected a current chapter.");
            PlayerProgressionData progression = PlayerAccountContext.Instance.GetCurrentProgression();
            var chapterStates = new List<ChapterChooserItemState>(ConfigManager.Instance.ChapterIndex.chapters.Count);
            foreach (ChapterIndexEntry chapter in ConfigManager.Instance.ChapterIndex.chapters)
                chapterStates.Add(new ChapterChooserItemState(chapter, chapter.chapterId == currentChapterId.Value, chapter.unlockLevelId <= progression.highestUnlockedLevel));

            await ApplicationPresentationService.Instance.RunWithLoading(() => UIManager.Instance.ShowChapterChooserAsync(chapterStates));
        }

        private void HandleChapterVisitRequested(int chapterId)
        {
            if (currentChapterId == chapterId)
            {
                UnityEngine.Debug.LogWarning($"Chapter {chapterId} is already active.");
                return;
            }

            ApplicationOperationRunner.Instance.Run(() => ChangeChapterAsync(chapterId));
        }

        private static void HandleChapterChooserCloseRequested()
        {
            UIManager.Instance.HideChapterChooser();
        }

        private async UniTask ChangeTabAsync(HomeMiddleTab tab)
        {
            await UIManager.Instance.DisplayHomeTabAsync(tab);
            activeMiddleTab = tab;
        }

        private async UniTask ChangeChapterAsync(int chapterId)
        {
            ChapterIndexEntry chapterEntry = GetChapterEntry(ConfigManager.Instance.ChapterIndex, chapterId);
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.General, async () =>
            {
                await addressableContentService.EnsureDownloadedAsync(chapterEntry.downloadLabel);
                ChapterContent chapterContent = await ConfigManager.Instance.GetChapterContentAsync(chapterId);
                PlayerProgressionData progression = PlayerAccountContext.Instance.GetCurrentProgression();
                await UIManager.Instance.ShowHome(chapterContent, progression);
                await ChangeTabAsync(HomeMiddleTab.Map);
                UIManager.Instance.HideChapterChooser();
                currentChapterId = chapterId;
            });
        }

        private void HandleSettingsRequested()
        {
            SettingsRequested?.Invoke();
        }

        private void HandleAddLifeRequested()
        {
            AddLifeRequested?.Invoke();
        }

        private void HandleAddCurrencyRequested()
        {
            AddCurrencyRequested?.Invoke();
        }

        private static ChapterIndexEntry ResolveLatestUnlockedChapter(ChapterIndex chapterIndex, int highestUnlockedLevel)
        {
            ChapterIndexEntry latestUnlockedChapter = null;
            foreach (ChapterIndexEntry chapter in chapterIndex.chapters)
            {
                if (chapter.unlockLevelId <= 0)
                    throw new InvalidOperationException($"Chapter {chapter.chapterId} has invalid unlock level {chapter.unlockLevelId}.");
                if (chapter.unlockLevelId > highestUnlockedLevel) continue;
                if (latestUnlockedChapter == null || chapter.unlockLevelId > latestUnlockedChapter.unlockLevelId)
                    latestUnlockedChapter = chapter;
            }

            return latestUnlockedChapter ?? throw new InvalidOperationException($"No chapter is unlocked for highest unlocked level {highestUnlockedLevel}.");
        }

        private static ChapterIndexEntry GetChapterEntry(ChapterIndex chapterIndex, int chapterId)
        {
            foreach (ChapterIndexEntry chapter in chapterIndex.chapters)
            {
                if (chapter.chapterId == chapterId) return chapter;
            }

            throw new InvalidOperationException($"Chapter index has no entry for chapter {chapterId}.");
        }

    }
}
