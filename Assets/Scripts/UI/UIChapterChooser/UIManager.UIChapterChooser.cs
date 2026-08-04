using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIChapterChooser chapterChooserPrefab;
        private UIChapterChooser chapterChooserInstance;

        public event Action<int> ChapterVisitRequested;
        public event Action ChapterChooserCloseRequested;

        public async UniTask ShowChapterChooserAsync(IReadOnlyList<ChapterChooserItemState> chapterStates)
        {
            if (chapterChooserInstance == null)
                chapterChooserInstance = Instantiate(chapterChooserPrefab, uiRoot);

            UnbindChapterChooser();
            BindChapterChooser();
            await chapterChooserInstance.ShowAsync(chapterStates);
        }

        public void HideChapterChooser()
        {
            UnbindChapterChooser();
            chapterChooserInstance?.Hide();
        }

        private void BindChapterChooser()
        {
            chapterChooserInstance.ChapterVisitRequested += HandleChapterVisitRequested;
            chapterChooserInstance.CloseRequested += HandleChapterChooserCloseRequested;
        }

        private void UnbindChapterChooser()
        {
            if (chapterChooserInstance == null) return;
            chapterChooserInstance.ChapterVisitRequested -= HandleChapterVisitRequested;
            chapterChooserInstance.CloseRequested -= HandleChapterChooserCloseRequested;
        }

        private void HandleChapterVisitRequested(int chapterId)
        {
            ChapterVisitRequested?.Invoke(chapterId);
        }

        private void HandleChapterChooserCloseRequested()
        {
            ChapterChooserCloseRequested?.Invoke();
        }
    }
}
