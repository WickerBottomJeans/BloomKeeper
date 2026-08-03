using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DefaultNamespace.UI
{
    public sealed class UIHome : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform middleSlot;
        [SerializeField] private UILevelSelect levelSelectPrefab;

        private AsyncOperationHandle<GameObject> topperInstanceHandle;
        private AsyncOperationHandle<GameObject> bottomInstanceHandle;
        private ChapterTopperView topperView;
        private ChapterBottomView bottomView;
        private UILevelSelect levelSelectInstance;
        private ChapterContent displayedChapter;
        private PlayerProgressionData displayedProgression;
        private string displayedTopperAddress;
        private string displayedBottomNavigationAddress;
        private int chapterDisplayRequestId;

        public event Action<int> LevelSelected;
        public event Action SettingsRequested;
        public event Action AddLifeRequested;
        public event Action AddCurrencyRequested;

        public async UniTask ShowAsync(ChapterContent chapter, PlayerProgressionData progression)
        {
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            if (progression == null) throw new ArgumentNullException(nameof(progression));

            gameObject.SetActive(true);
            ChapterDefinition definition = chapter.Definition;
            if (displayedTopperAddress != definition.topperPrefabAddress || displayedBottomNavigationAddress != definition.bottomNavigationPrefabAddress)
                await DisplayChapterViewsAsync(definition.topperPrefabAddress, definition.bottomNavigationPrefabAddress);

            displayedChapter = chapter;
            displayedProgression = progression;
            await ShowMapAsync();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void DisplayLives(int value)
        {
            if (topperView == null) throw new InvalidOperationException("UIHome cannot display lives before its chapter Topper has loaded.");
            topperView.DisplayLives(value);
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

        private async UniTask ShowMapAsync()
        {
            if (displayedChapter == null) throw new InvalidOperationException("UIHome cannot display the Map tab before receiving chapter content.");
            if (displayedProgression == null) throw new InvalidOperationException("UIHome cannot display the Map tab before receiving progression data.");
            if (levelSelectInstance == null)
            {
                levelSelectInstance = Instantiate(levelSelectPrefab, middleSlot, false);
                levelSelectInstance.OnLevelSelected += HandleLevelSelected;
            }

            await levelSelectInstance.Show(displayedChapter, displayedProgression);
            await levelSelectInstance.WaitForInitialBackgroundLoaded();
        }

        private void BindChapterViews()
        {
            topperView.AddLifeRequested += HandleAddLifeRequested;
            topperView.AddCurrencyRequested += HandleAddCurrencyRequested;
            bottomView.MapRequested += HandleMapRequested;
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
                bottomView.MapRequested -= HandleMapRequested;
                bottomView.SettingsRequested -= HandleSettingsRequested;
            }
        }

        private void HandleMapRequested()
        {
            ShowMapAsync().Forget();
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

        private static void ReleasePendingInstance(AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid()) return;
            if (handle.Status == AsyncOperationStatus.Succeeded) Addressables.ReleaseInstance(handle);
            else Addressables.Release(handle);
        }

        private void OnDestroy()
        {
            chapterDisplayRequestId++;
            if (levelSelectInstance != null) levelSelectInstance.OnLevelSelected -= HandleLevelSelected;
            ReleaseChapterViews();
        }
    }
}
