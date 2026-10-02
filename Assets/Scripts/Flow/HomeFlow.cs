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
        private readonly HomeMapFlow homeMapFlow;
        private readonly HomeShopFlow homeShopFlow;
        private readonly HomeTutorialFlow homeTutorialFlow;
        private readonly AutomaticGooglePlayReviewFlow automaticGooglePlayReviewFlow = new();
        private HomeMiddleTab? activeMiddleTab;
        private int? currentChapterId;
        private CancellationTokenSource livesDisplayCancellation;
        private CancellationTokenSource automaticGooglePlayReviewCancellation;

        public event Action<int> StartLevelRequested;
        public event Action SettingsRequested;

        /// <summary>
        /// Creates the home flow and its child flows.
        /// </summary>
        public HomeFlow(AddressableContentService addressableContentService, PlayerLivesPresentationService playerLivesPresentationService, HomeTutorialConfig homeTutorialConfig)
        {
            this.addressableContentService = addressableContentService ?? throw new ArgumentNullException(nameof(addressableContentService));
            this.playerLivesPresentationService = playerLivesPresentationService ?? throw new ArgumentNullException(nameof(playerLivesPresentationService));
            homeMapFlow = new HomeMapFlow();
            homeShopFlow = new HomeShopFlow(playerLivesPresentationService);
            homeTutorialFlow = new HomeTutorialFlow(homeTutorialConfig);
        }

        /// <summary>
        /// [Duong] Load configs and addresable content and show home UI
        /// </summary>
        public async UniTask Enter()
        {
            automaticGooglePlayReviewCancellation = CancellationTokenSource.CreateLinkedTokenSource(UnityEngine.Application.exitCancellationToken);

            // [Duong] Bind Home events.
            UIManager.Instance.LevelSelected += HandleLevelSelected;
            UIManager.Instance.HomeTabRequested += HandleHomeTabRequested;
            UIManager.Instance.ChapterVisitRequested += HandleChapterVisitRequested;
            UIManager.Instance.ChapterChooserCloseRequested += HandleChapterChooserCloseRequested;
            UIManager.Instance.HomeChapterChooserShown += homeTutorialFlow.HandleHomeChapterChooserShown;
            UIManager.Instance.HomeChapterChooserHidden += homeTutorialFlow.HandleHomeChapterChooserHidden;
            UIManager.Instance.SettingsRequested += HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested += HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested += HandleAddCurrencyRequested;
            homeShopFlow.Enter();

            playerLivesPresentationService.ServerLivesSnapshotChanged += HandleServerLivesSnapshotChanged;

            // [Duong] Resolve the current chapter.
            PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
            PlayerProgressionData progression = account.Progression;
            if (!currentChapterId.HasValue) currentChapterId = PlayerPrefsStore.LoadLastSelectedChapterId();
            ChapterIndexEntry chapterEntry = currentChapterId.HasValue
                ? ConfigManager.Instance.ChapterIndex.GetEntry(currentChapterId.Value)
                : ConfigManager.Instance.ChapterIndex.GetLatestUnlockedEntry(progression.highestUnlockedLevel);
            if (chapterEntry.unlockLevelId > progression.highestUnlockedLevel)
                throw new InvalidOperationException($"Stored chapter {chapterEntry.chapterId} requires level {chapterEntry.unlockLevelId}, but the highest unlocked level is {progression.highestUnlockedLevel}.");

            // [Duong] Prepare chapter content.
            await addressableContentService.EnsureDownloadedAsync(chapterEntry.downloadLabel);
            ChapterContent chapterContent = await ConfigManager.Instance.GetChapterContentAsync(chapterEntry.chapterId);

            // [Duong] Enter the initial Map tab.
            await UIManager.Instance.ShowHome(chapterContent.Definition.topperPrefabAddress, chapterContent.Definition.bottomNavigationPrefabAddress, playerLivesPresentationService.CreateCurrentLivesViewData(DateTimeOffset.UtcNow), account.PlayerInventory.DiamondQuantity);
            homeMapFlow.SetCurrentMapChapter(chapterContent);
            await ChangeTabAsync(HomeMiddleTab.Map);
            SetCurrentChapter(chapterEntry.chapterId);

            // [Duong] Start the lives display loop.
            livesDisplayCancellation = CancellationTokenSource.CreateLinkedTokenSource(UnityEngine.Application.exitCancellationToken);
            UpdateLivesDisplayLoop(livesDisplayCancellation.Token).Forget();

            // Start the Home introduction.
            homeTutorialFlow.StartHomeTutorial();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.HomeTutorialReplayRequested += HandleHomeTutorialReplayRequested;
#endif
        }

        /// <summary>
        /// [Duong] Unbind stuff and hide UI
        /// </summary>
        public void Exit()
        {
            CancelPendingAutomaticGooglePlayReview();
            automaticGooglePlayReviewCancellation.Dispose();
            automaticGooglePlayReviewCancellation = null;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.HomeTutorialReplayRequested -= HandleHomeTutorialReplayRequested;
#endif
            homeTutorialFlow.StopHomeTutorial();
            UIManager.Instance.LevelSelected -= HandleLevelSelected;
            UIManager.Instance.HomeTabRequested -= HandleHomeTabRequested;
            UIManager.Instance.ChapterVisitRequested -= HandleChapterVisitRequested;
            UIManager.Instance.ChapterChooserCloseRequested -= HandleChapterChooserCloseRequested;
            UIManager.Instance.HomeChapterChooserShown -= homeTutorialFlow.HandleHomeChapterChooserShown;
            UIManager.Instance.HomeChapterChooserHidden -= homeTutorialFlow.HandleHomeChapterChooserHidden;
            UIManager.Instance.SettingsRequested -= HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested -= HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested -= HandleAddCurrencyRequested;
            homeShopFlow.Exit();
            playerLivesPresentationService.ServerLivesSnapshotChanged -= HandleServerLivesSnapshotChanged;
            livesDisplayCancellation.Cancel();
            livesDisplayCancellation.Dispose();
            livesDisplayCancellation = null;
            UIManager.Instance.HideHome();
            activeMiddleTab = null;
        }

        /// <summary>
        /// Checks the automatic review opportunity after Home loading has finished.
        /// </summary>
        public async UniTask HandleHomeReadyAsync()
        {
            if (homeTutorialFlow.IsHomeTutorialActive) return;

            CancellationToken cancellationToken = automaticGooglePlayReviewCancellation.Token;
            try
            {
                await automaticGooglePlayReviewFlow.TryRequestAutomaticGooglePlayReviewAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }

        private void CancelPendingAutomaticGooglePlayReview()
        {
            automaticGooglePlayReviewCancellation?.Cancel();
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
            if (homeTutorialFlow.IsHomeTutorialActive) return;
            CancelPendingAutomaticGooglePlayReview();
            StartLevelRequested?.Invoke(levelId);
        }

        /// <summary>
        /// [Duong] Handles a Home tab request, opening the chapter chooser when Map is already active
        /// </summary>
        private void HandleHomeTabRequested(HomeMiddleTab tab)
        {
            if (!homeTutorialFlow.TryBeginHomeTutorialTabNavigation(tab)) return;
            CancelPendingAutomaticGooglePlayReview();
            //[Duong] Clicking the active Map tab opens the chapter chooser
            if (tab == HomeMiddleTab.Map && activeMiddleTab == HomeMiddleTab.Map)
            {
                ApplicationOperationRunner.Instance.Run(OpenChapterChooserAsync);
                return;
            }

            ApplicationOperationRunner.Instance.Run(() => ChangeTabAsync(tab));
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void HandleHomeTutorialReplayRequested()
        {
            CancelPendingAutomaticGooglePlayReview();
            ApplicationOperationRunner.Instance.Run(async () =>
            {
                homeTutorialFlow.StopHomeTutorial();
                await ChangeTabAsync(HomeMiddleTab.Map);
                homeTutorialFlow.StartHomeTutorialPreview();
            });
        }
#endif

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
            if (homeTutorialFlow.IsHomeTutorialActive) return;
            if (currentChapterId == chapterId)
            {
                UnityEngine.Debug.LogWarning($"Chapter {chapterId} is already active.");
                return;
            }

            CancelPendingAutomaticGooglePlayReview();
            ApplicationOperationRunner.Instance.Run(() => ChangeChapterAsync(chapterId));
        }

        private  void HandleChapterChooserCloseRequested()
        {
            if (!homeTutorialFlow.TryBeginHomeTutorialChapterClose()) return;
            UIManager.Instance.HideChapterChooser();
        }

        private async UniTask ChangeTabAsync(HomeMiddleTab tab)
        {
            switch (tab)
            {
                case HomeMiddleTab.Map:
                    await homeMapFlow.EnterMapAsync();
                    break;
                case HomeMiddleTab.Shop:
                    bool shopDisplayed = await homeShopFlow.TryEnterShopAsync();
                    if (shopDisplayed) activeMiddleTab = HomeMiddleTab.Shop;
                    homeTutorialFlow.HandleHomeShopNavigationFinished(shopDisplayed);
                    if (!shopDisplayed) return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown Home middle tab.");
            }

            activeMiddleTab = tab;
        }

        private async UniTask ChangeChapterAsync(int chapterId)
        {
            ChapterIndexEntry chapterEntry = ConfigManager.Instance.ChapterIndex.GetEntry(chapterId);
            await ApplicationPresentationService.Instance.RunWithLoading(async () =>
            {
                await addressableContentService.EnsureDownloadedAsync(chapterEntry.downloadLabel);
                ChapterContent chapterContent = await ConfigManager.Instance.GetChapterContentAsync(chapterId);
                await UIManager.Instance.ShowHome(chapterContent.Definition.topperPrefabAddress, chapterContent.Definition.bottomNavigationPrefabAddress, playerLivesPresentationService.CreateCurrentLivesViewData(DateTimeOffset.UtcNow), PlayerAccountContext.Instance.GetCurrentPlayerInventory().DiamondQuantity);
                homeMapFlow.SetCurrentMapChapter(chapterContent);
                await ChangeTabAsync(HomeMiddleTab.Map);
                UIManager.Instance.HideChapterChooser();
                SetCurrentChapter(chapterId);
            });
        }

        private void HandleSettingsRequested()
        {
            if (homeTutorialFlow.IsHomeTutorialActive) return;
            CancelPendingAutomaticGooglePlayReview();
            SettingsRequested?.Invoke();
        }

        private void HandleAddLifeRequested()
        {
            if (homeTutorialFlow.IsHomeTutorialActive) return;
            CancelPendingAutomaticGooglePlayReview();
            ApplicationOperationRunner.Instance.Run(() => ChangeTabAsync(HomeMiddleTab.Shop));
        }

        private void HandleAddCurrencyRequested()
        {
            if (homeTutorialFlow.IsHomeTutorialActive) return;
            CancelPendingAutomaticGooglePlayReview();
            ApplicationOperationRunner.Instance.Run(() => DialogManager.Instance.RunOkDialog("Earn diamonds", "You can earn diamonds just by playing the game. Keep playing to collect more!"));
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
                    cancellationToken.ThrowIfCancellationRequested();
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
