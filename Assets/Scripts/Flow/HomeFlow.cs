using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeFlow
    {
        //TODO: remove this number, we dont store this in a build
        private const int InitialChapterId = 1;
        private readonly IChapterContentProvider chapterContentProvider;

        public event Action<int> StartLevelRequested;
        public event Action SettingsRequested;
        public event Action AddLifeRequested;
        public event Action AddCurrencyRequested;

        public HomeFlow(IChapterContentProvider chapterContentProvider)
        {
            this.chapterContentProvider = chapterContentProvider ?? throw new ArgumentNullException(nameof(chapterContentProvider));
        }

        public async UniTask Enter()
        {
            ChapterContent chapterContent = await chapterContentProvider.LoadChapterAsync(InitialChapterId);
            UIManager.Instance.LevelSelected += HandleLevelSelected;
            UIManager.Instance.SettingsRequested += HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested += HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested += HandleAddCurrencyRequested;
            await UIManager.Instance.ShowHome(chapterContent, PlayerAccountContext.Instance.GetCurrentProgression());
        }

        public void Exit()
        {
            UIManager.Instance.LevelSelected -= HandleLevelSelected;
            UIManager.Instance.SettingsRequested -= HandleSettingsRequested;
            UIManager.Instance.AddLifeRequested -= HandleAddLifeRequested;
            UIManager.Instance.AddCurrencyRequested -= HandleAddCurrencyRequested;
            UIManager.Instance.HideHome();
        }

        private void HandleLevelSelected(int levelId)
        {
            StartLevelRequested?.Invoke(levelId);
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

    }
}
