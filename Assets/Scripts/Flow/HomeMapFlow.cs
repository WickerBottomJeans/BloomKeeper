using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeMapFlow
    {
        private ChapterContent currentMapChapter;

        public void SetCurrentMapChapter(ChapterContent chapter)
        {
            currentMapChapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
        }

        public async UniTask EnterMapAsync()
        {
            if (currentMapChapter == null) throw new InvalidOperationException("Cannot enter the Home Map before its chapter content is available.");

            PlayerProgressionData progression = PlayerAccountContext.Instance.GetCurrentProgression();
            await UIManager.Instance.DisplayHomeMapAsync(currentMapChapter, progression);
        }
    }
}
