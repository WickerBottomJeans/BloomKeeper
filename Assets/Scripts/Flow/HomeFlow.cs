using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Settings;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Manages the Home screen, chapter loading, chapter selection, tab navigation, and UI events.
    /// </summary>
    public class HomeFlow
    {
        private readonly AddressableContentService addressableContentService;
        private readonly PlayerLivesPresentationService playerLivesPresentationService;
        private HomeMiddleTab? activeMiddleTab;
        private int? currentChapterId;
        private CancellationTokenSource livesDisplayCancellation;

        public event Action<int> StartLevelRequested;
        public event Action SettingsRequested;
        public event Action AddLifeRequested;
        public event Action AddCurrencyRequested;

        public HomeFlow(AddressableContentService addressableContentService, PlayerLivesPresentationService playerLivesPresentationService)
        {
            this.addressableContentService = addressableContentService ?? throw new ArgumentNullException(nameof(addressableContentService));
            this.playerLivesPresentationService = playerLivesPresentationService ?? throw new ArgumentNullException(nameof(playerLivesPresentationService));
        }

        /// <summary>
        /// [Duong] Load configs and addresable content and show home UI
        /// </summary>
        public async UniTask Enter()
        {
            UIManager.Instance.LevelSelected += HandleLevelSelected;
            UIManager.Instance.HomeTabRequested += HandleHomeTabRequested;
            UIManager.Instance.ChapterVisitRequested += HandleChapterVisitRequested;
            UIManager.Instance.ChapterChooserCloseRequested += HandleChapterChooserCloseRequested;
            UIManager.Instance.SettingsRequested += HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested += HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested += HandleAddCurrencyRequested;

            playerLivesPresentationService.ServerLivesSnapshotChanged += HandleServerLivesSnapshotChanged;
            PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
            PlayerProgressionData progression = account.Progression;
            if (!currentChapterId.HasValue) currentChapterId = PlayerPrefsStore.LoadLastSelectedChapterId();
            ChapterIndexEntry chapterEntry = currentChapterId.HasValue
                ? ConfigManager.Instance.ChapterIndex.GetEntry(currentChapterId.Value)
                : ConfigManager.Instance.ChapterIndex.GetLatestUnlockedEntry(progression.highestUnlockedLevel);
            if (chapterEntry.unlockLevelId > progression.highestUnlockedLevel)
                throw new InvalidOperationException($"Stored chapter {chapterEntry.chapterId} requires level {chapterEntry.unlockLevelId}, but the highest unlocked level is {progression.highestUnlockedLevel}.");
            await addressableContentService.EnsureDownloadedAsync(chapterEntry.downloadLabel);
            ChapterContent chapterContent = await ConfigManager.Instance.GetChapterContentAsync(chapterEntry.chapterId);
            await UIManager.Instance.ShowHome(chapterContent, progression, playerLivesPresentationService.CreateCurrentLivesViewData(DateTimeOffset.UtcNow));
            await ChangeTabAsync(HomeMiddleTab.Map);
            SetCurrentChapter(chapterEntry.chapterId);
            livesDisplayCancellation = new CancellationTokenSource();
            UpdateLivesDisplayLoop(livesDisplayCancellation.Token).Forget();
        }

        /// <summary>
        /// [Duong] Unbind stuff and hide UI
        /// </summary>
        public void Exit()
        {
            UIManager.Instance.LevelSelected -= HandleLevelSelected;
            UIManager.Instance.HomeTabRequested -= HandleHomeTabRequested;
            UIManager.Instance.ChapterVisitRequested -= HandleChapterVisitRequested;
            UIManager.Instance.ChapterChooserCloseRequested -= HandleChapterChooserCloseRequested;
            UIManager.Instance.SettingsRequested -= HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested -= HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested -= HandleAddCurrencyRequested;
            playerLivesPresentationService.ServerLivesSnapshotChanged -= HandleServerLivesSnapshotChanged;
            livesDisplayCancellation.Cancel();
            livesDisplayCancellation.Dispose();
            livesDisplayCancellation = null;
            UIManager.Instance.HideHome();
            activeMiddleTab = null;
        }

        /// <summary>
        /// [Duong] Sets the current Home chapter and saves it to PlayerPrefs.
        /// </summary>
        public void SetCurrentChapter(int chapterId)
        {
            PlayerPrefsStore.SaveLastSelectedChapterId(chapterId);
            currentChapterId = chapterId;
        }

        /// <summary>
        /// [Duong] Forwards the UI's request to start the selected level
        /// </summary>
        private void HandleLevelSelected(int levelId)
        {
            StartLevelRequested?.Invoke(levelId);
        }

        /// <summary>
        /// [Duong] Handles a Home tab request, opening the chapter chooser when Map is already active
        /// </summary>
        private void HandleHomeTabRequested(HomeMiddleTab tab)
        {
            //[Duong] Clicking the active Map tab opens the chapter chooser
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

            await ApplicationPresentationService.Instance.RunWithLoading(() => UIManager.Instance.PrepareChapterChooserAsync(chapterStates));
            UIManager.Instance.ShowChapterChooser();
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

        private  void HandleChapterChooserCloseRequested()
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
            ChapterIndexEntry chapterEntry = ConfigManager.Instance.ChapterIndex.GetEntry(chapterId);
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.General, async () =>
            {
                await addressableContentService.EnsureDownloadedAsync(chapterEntry.downloadLabel);
                ChapterContent chapterContent = await ConfigManager.Instance.GetChapterContentAsync(chapterId);
                PlayerProgressionData progression = PlayerAccountContext.Instance.GetCurrentProgression();
                await UIManager.Instance.ShowHome(chapterContent, progression, playerLivesPresentationService.CreateCurrentLivesViewData(DateTimeOffset.UtcNow));
                await ChangeTabAsync(HomeMiddleTab.Map);
                UIManager.Instance.HideChapterChooser();
                SetCurrentChapter(chapterId);
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

        private void HandleServerLivesSnapshotChanged()
        {
            UIManager.Instance.DisplayHomeLives(playerLivesPresentationService.CreateCurrentLivesViewData(DateTimeOffset.UtcNow));
        }

        private async UniTask UpdateLivesDisplayLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    UIManager.Instance.DisplayHomeLives(playerLivesPresentationService.CreateCurrentLivesViewData(DateTimeOffset.UtcNow));
                    await UniTask.Delay(TimeSpan.FromSeconds(1), true, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

    }
}
