using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class UIChapterChooser : MonoBehaviour
    {
        [SerializeField] private HorizontalPagedScrollView pagedScrollView;
        [SerializeField] private UIChapterView chapterViewTemplate;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private int defaultPoolCapacity = 3;
        [SerializeField] private int maxPoolSize = 5;

        private IReadOnlyList<ChapterChooserItemState> chapterStates;
        private HorizontalScrollPool<UIChapterView> chapterViewPool;
        private UniTask? currentChapterInitialization;

        public event Action<int> ChapterVisitRequested;
        public event Action CloseRequested;

        private void Awake()
        {
            chapterViewTemplate.gameObject.SetActive(false);
            dimmerButton.onClick.AddListener(HandleDimmerClicked);
        }

        public async UniTask ShowAsync(IReadOnlyList<ChapterChooserItemState> chapterStates)
        {
            gameObject.SetActive(true);
            DisposeDisplayedChapters();
            this.chapterStates = chapterStates;

            int currentChapterIndex = -1;
            for (int index = 0; index < chapterStates.Count; index++)
            {
                if (!chapterStates[index].IsCurrent) continue;
                currentChapterIndex = index;
                break;
            }

            if (currentChapterIndex < 0)
                throw new InvalidOperationException("Chapter chooser requires one current chapter.");

            pagedScrollView.RefreshPages(chapterStates.Count);
            pagedScrollView.SetPage(currentChapterIndex, false);
            currentChapterInitialization = null;
            chapterViewPool = new HorizontalScrollPool<UIChapterView>(pagedScrollView.Pages, pagedScrollView.Viewport, pagedScrollView.ScrollRect, chapterViewTemplate, chapterStates.Count, pagedScrollView.GetPagePosition, _ => pagedScrollView.PageWidth * 0.5f, index => pagedScrollView.GetPageSlot(index), BindViewEvents, ShowView, HideView, defaultPoolCapacity, maxPoolSize);
            if (!currentChapterInitialization.HasValue)
                throw new InvalidOperationException("Chapter chooser pool did not create the current chapter view.");

            try
            {
                await currentChapterInitialization.Value;
            }
            finally
            {
                currentChapterInitialization = null;
            }
        }

        public void Hide()
        {
            DisposeDisplayedChapters();
            gameObject.SetActive(false);
        }

        private void BindViewEvents(UIChapterView view)
        {
            view.VisitRequested += chapterId => ChapterVisitRequested?.Invoke(chapterId);
        }

        private void ShowView(UIChapterView view, int index)
        {
            UniTask initialization = view.Init(chapterStates[index]);
            if (chapterStates[index].IsCurrent)
            {
                currentChapterInitialization = initialization;
                return;
            }

            initialization.Forget();
        }

        private static void HideView(UIChapterView view)
        {
            view.ResetForPool();
        }

        private void DisposeDisplayedChapters()
        {
            chapterViewPool?.Dispose();
            chapterViewPool = null;
            currentChapterInitialization = null;
            pagedScrollView.RefreshPages(0);
            chapterStates = null;
        }

        private void HandleDimmerClicked()
        {
            CloseRequested?.Invoke();
        }

        private void OnDestroy()
        {
            dimmerButton.onClick.RemoveListener(HandleDimmerClicked);
            DisposeDisplayedChapters();
        }
    }
}
