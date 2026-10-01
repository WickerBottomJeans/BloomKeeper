using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UITutorial tutorialPrefab;
        private UITutorial tutorialInstance;

        public event Action TutorialContinueRequested;
        public event Action TutorialSkipRequested;

        public void HideTutorial()
        {
            if (tutorialInstance == null) return;
            tutorialInstance.ContinueRequested -= HandleTutorialContinueRequested;
            tutorialInstance.SkipRequested -= HandleTutorialSkipRequested;
            tutorialInstance.HideTutorial();
        }

        private void DisplayTutorial(UITutorialTarget highlightTarget, string message, string continueButtonLabel, bool allowTargetInput)
        {
            GetPanel(ref tutorialInstance, tutorialPrefab, uiRoot);
            tutorialInstance.ContinueRequested -= HandleTutorialContinueRequested;
            tutorialInstance.ContinueRequested += HandleTutorialContinueRequested;
            tutorialInstance.SkipRequested -= HandleTutorialSkipRequested;
            tutorialInstance.SkipRequested += HandleTutorialSkipRequested;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            tutorialInstance.DisplayTutorial(highlightTarget, uiCamera, message, continueButtonLabel, allowTargetInput);
        }

        private void HandleTutorialContinueRequested()
        {
            TutorialContinueRequested?.Invoke();
        }

        private void HandleTutorialSkipRequested()
        {
            TutorialSkipRequested?.Invoke();
        }
    }
}
