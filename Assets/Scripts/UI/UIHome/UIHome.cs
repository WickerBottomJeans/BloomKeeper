using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DefaultNamespace.UI
{
    public class UIHome : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform middleSlot;
        [SerializeField] private RectTransform fullScreenRoot;
        [SerializeField] private UILevelSelect levelSelectPrefab;
        [SerializeField] private UIFriendsView friendsPrefab;
        [SerializeField] private UIMainShopTab shopPrefab;
        [SerializeField] private UIChapterChooser chapterChooser;

        private AsyncOperationHandle<GameObject> topperInstanceHandle;
        private AsyncOperationHandle<GameObject> bottomInstanceHandle;
        private ChapterTopperView topperView;
        private ChapterBottomView bottomView;
        private UILevelSelect levelSelectInstance;
        private UIFriendsView friendsInstance;
        private UIMainShopTab shopInstance;
        private string displayedTopperAddress;
        private string displayedBottomNavigationAddress;
        private int chapterDisplayRequestId;

        public event Action<int> LevelSelected;
        public event Action<HomeMiddleTab> TabRequested;
        public event Action SettingsRequested;
        public event Action AddLifeRequested;
        public event Action AddCurrencyRequested;
        public event Action<int> ChapterVisitRequested;
        public event Action ChapterChooserCloseRequested;
        /// <summary>
        /// Shop offer selected in the shop UI.
        /// </summary>
        public event Action<string> ShopOfferBuyRequested;

        private void Awake()
        {
            chapterChooser.ChapterVisitRequested += HandleChapterVisitRequested;
            chapterChooser.CloseRequested += HandleChapterChooserCloseRequested;
            chapterChooser.HideChapterChooser();
        }

        public async UniTask ShowAsync(string topperPrefabAddress, string bottomNavigationPrefabAddress, PlayerLivesViewData lives, int diamondQuantity)
        {
            if (lives == null) throw new ArgumentNullException(nameof(lives));

            gameObject.SetActive(true);
            if (displayedTopperAddress != topperPrefabAddress || displayedBottomNavigationAddress != bottomNavigationPrefabAddress)
                await DisplayChapterViewsAsync(topperPrefabAddress, bottomNavigationPrefabAddress);

            topperView.DisplayLives(lives);
            topperView.DisplayCurrency(diamondQuantity);
        }

        public async UniTask DisplayMapAsync(ChapterContent chapter, PlayerProgressionData progression)
        {
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            if (progression == null) throw new ArgumentNullException(nameof(progression));

            friendsInstance?.Hide();
            shopInstance?.Hide();
            await ShowMapAsync(chapter, progression);
        }

        public void DisplayFriends()
        {
            levelSelectInstance?.Hide();
            shopInstance?.Hide();
            if (friendsInstance == null) friendsInstance = Instantiate(friendsPrefab, middleSlot, false);
            friendsInstance.Show();
        }

        /// <summary>
        /// Shows the shop in the middle of Home.
        /// </summary>
        public void DisplayShop(LoadShopResponse mainShopResponse)
        {
            levelSelectInstance?.Hide();
            friendsInstance?.Hide();
            if (shopInstance == null)
            {
                shopInstance = Instantiate(shopPrefab, middleSlot, false);
                shopInstance.BuyRequested += HandleShopOfferBuyRequested;
            }
            shopInstance.DisplayMainShop(mainShopResponse);
        }

        public async UniTask PrepareChapterChooserAsync(IReadOnlyList<ChapterChooserItemState> chapterStates)
        {
            chapterChooser.HideForPreparation();
            await chapterChooser.PrepareAsync(chapterStates);
        }

        public void ShowChapterChooser()
        {
            chapterChooser.ShowChapterChooser();
        }

        public void HideChapterChooser()
        {
            chapterChooser.HideChapterChooser();
        }

        public void Hide()
        {
            chapterChooser.HideChapterChooser();
            gameObject.SetActive(false);
        }

        public void DisplayLives(PlayerLivesViewData lives)
        {
            if (topperView == null) throw new InvalidOperationException("UIHome cannot display lives before its chapter Topper has loaded.");
            topperView.DisplayLives(lives);
        }

        public void DisplayCurrency(int value)
        {
            if (topperView == null) throw new InvalidOperationException("UIHome cannot display currency before its chapter Topper has loaded.");
            topperView.DisplayCurrency(value);
        }

        public void DisplayAvatar(Sprite avatar)
        {
            if (topperView == null) throw new InvalidOperationException("UIHome cannot display an avatar before its chapter Topper has loaded.");
            topperView.DisplayAvatar(avatar);
        }

        private async UniTask DisplayChapterViewsAsync(string topperAddress, string bottomNavigationAddress)
        {
            if (string.IsNullOrWhiteSpace(topperAddress)) throw new ArgumentException("A Topper Addressables address is required.", nameof(topperAddress));
            if (string.IsNullOrWhiteSpace(bottomNavigationAddress)) throw new ArgumentException("A Bottom Navigation Addressables address is required.", nameof(bottomNavigationAddress));

            int requestId = ++chapterDisplayRequestId;
            AsyncOperationHandle<GameObject> newTopperHandle = Addressables.InstantiateAsync(topperAddress, content, false);
            AsyncOperationHandle<GameObject> newBottomHandle = Addressables.InstantiateAsync(bottomNavigationAddress, content, false);
            try
            {
                GameObject newTopperObject = await newTopperHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
                newTopperObject.SetActive(false);
                GameObject newBottomObject = await newBottomHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
                newBottomObject.SetActive(false);
                if (requestId != chapterDisplayRequestId)
                    throw new OperationCanceledException("UIHome chapter display request was superseded by a newer request.");

                ChapterTopperView newTopperView = newTopperObject.GetComponent<ChapterTopperView>();
                if (newTopperView == null)
                    throw new InvalidOperationException($"Addressable prefab '{topperAddress}' does not contain ChapterTopperView on its root.");
                ChapterBottomView newBottomView = newBottomObject.GetComponent<ChapterBottomView>();
                if (newBottomView == null)
                    throw new InvalidOperationException($"Addressable prefab '{bottomNavigationAddress}' does not contain ChapterBottomView on its root.");

                newTopperView.SetBleedTarget(fullScreenRoot);
                newBottomView.SetBleedTarget(fullScreenRoot);

                ReleaseChapterViews();
                newTopperObject.transform.SetSiblingIndex(0);
                newBottomObject.transform.SetAsLastSibling();
                topperInstanceHandle = newTopperHandle;
                bottomInstanceHandle = newBottomHandle;
                topperView = newTopperView;
                bottomView = newBottomView;
                displayedTopperAddress = topperAddress;
                displayedBottomNavigationAddress = bottomNavigationAddress;
                BindChapterViews();
                newTopperObject.SetActive(true);
                newBottomObject.SetActive(true);
                newTopperHandle = default;
                newBottomHandle = default;
            }
            catch
            {
                ReleasePendingInstance(newTopperHandle);
                ReleasePendingInstance(newBottomHandle);
                throw;
            }
        }

        private async UniTask ShowMapAsync(ChapterContent chapter, PlayerProgressionData progression)
        {
            if (levelSelectInstance == null)
            {
                levelSelectInstance = Instantiate(levelSelectPrefab, middleSlot, false);
                levelSelectInstance.OnLevelSelected += HandleLevelSelected;
            }

            await levelSelectInstance.Show(chapter, progression);
            await levelSelectInstance.WaitForInitialBackgroundLoaded();
        }

        private void BindChapterViews()
        {
            topperView.AddLifeRequested += HandleAddLifeRequested;
            topperView.AddCurrencyRequested += HandleAddCurrencyRequested;
            bottomView.TabRequested += HandleTabRequested;
            bottomView.SettingsRequested += HandleSettingsRequested;
        }

        private void UnbindChapterViews()
        {
            if (topperView != null)
            {
                topperView.AddLifeRequested -= HandleAddLifeRequested;
                topperView.AddCurrencyRequested -= HandleAddCurrencyRequested;
            }
            if (bottomView != null)
            {
                bottomView.TabRequested -= HandleTabRequested;
                bottomView.SettingsRequested -= HandleSettingsRequested;
            }
        }

        private void HandleTabRequested(HomeMiddleTab tab)
        {
            TabRequested?.Invoke(tab);
        }

        private void HandleLevelSelected(int levelId)
        {
            LevelSelected?.Invoke(levelId);
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

        private void HandleChapterVisitRequested(int chapterId)
        {
            ChapterVisitRequested?.Invoke(chapterId);
        }

        private void HandleChapterChooserCloseRequested()
        {
            ChapterChooserCloseRequested?.Invoke();
        }

        /// <summary>
        /// Raises ShopOfferBuyRequested with the selected offer ID.
        /// </summary>
        private void HandleShopOfferBuyRequested(string offerId)
        {
            ShopOfferBuyRequested?.Invoke(offerId);
        }

        private void ReleaseChapterViews()
        {
            UnbindChapterViews();
            if (topperInstanceHandle.IsValid()) Addressables.ReleaseInstance(topperInstanceHandle);
            if (bottomInstanceHandle.IsValid()) Addressables.ReleaseInstance(bottomInstanceHandle);
            topperInstanceHandle = default;
            bottomInstanceHandle = default;
            topperView = null;
            bottomView = null;
            displayedTopperAddress = null;
            displayedBottomNavigationAddress = null;
        }

        private  void ReleasePendingInstance(AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid()) return;
            if (handle.Status == AsyncOperationStatus.Succeeded) Addressables.ReleaseInstance(handle);
            else Addressables.Release(handle);
        }

        private void OnDestroy()
        {
            chapterDisplayRequestId++;
            chapterChooser.ChapterVisitRequested -= HandleChapterVisitRequested;
            chapterChooser.CloseRequested -= HandleChapterChooserCloseRequested;
            if (levelSelectInstance != null) levelSelectInstance.OnLevelSelected -= HandleLevelSelected;
            if (shopInstance != null) shopInstance.BuyRequested -= HandleShopOfferBuyRequested;
            ReleaseChapterViews();
        }
    }
}
