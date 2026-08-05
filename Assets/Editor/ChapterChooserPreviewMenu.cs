using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEditor;

namespace DefaultNamespace.Editor
{
    public static class ChapterChooserDebugMenu
    {
        private const int PreviewChapterCount = 20;

        [MenuItem("Tools/BloomKeeper/Debug/Show 20 Fake Chapters")]
        private static void ShowFakeChapters()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Fake chapter preview requires Play Mode.");

            ChapterIndex chapterIndex = ConfigManager.Instance.ChapterIndex;
            if (chapterIndex.chapters.Count == 0)
                throw new InvalidOperationException("Fake chapter preview requires at least one real chapter entry.");

            ChapterIndexEntry template = chapterIndex.chapters[0];
            var fakeChapterStates = new List<ChapterChooserItemState>(PreviewChapterCount);
            for (int index = 0; index < PreviewChapterCount; index++)
            {
                var fakeChapter = new ChapterIndexEntry
                {
                    chapterId = template.chapterId,
                    displayName = $"Fake Chapter {index + 1}",
                    description = $"Chapter chooser pool preview item {index + 1}.",
                    configPath = template.configPath,
                    chooserImageAddress = template.chooserImageAddress,
                    downloadLabel = template.downloadLabel,
                    unlockLevelId = template.unlockLevelId
                };
                fakeChapterStates.Add(new ChapterChooserItemState(fakeChapter, index == 0, true));
            }

            ApplicationOperationRunner.Instance.Run(() => ShowFakeChaptersAsync(fakeChapterStates));
        }

        private static async UniTask ShowFakeChaptersAsync(IReadOnlyList<ChapterChooserItemState> chapterStates)
        {
            await ApplicationPresentationService.Instance.RunWithLoading(() => UIManager.Instance.PrepareChapterChooserAsync(chapterStates));
            UIManager.Instance.ShowChapterChooser();
        }
    }
}
