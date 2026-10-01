using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Home navigation buttons.
    /// </summary>
    public class ChapterBottomView : MonoBehaviour
    {
        [SerializeField] private Button mapButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private UITutorialTarget mapTutorialTarget;
        [SerializeField] private UITutorialTarget shopTutorialTarget;
        [SerializeField] private SafeAreaContentFitter[] safeAreaContentFitters = Array.Empty<SafeAreaContentFitter>();

        /// <summary>
        /// Home tab selected by the player.
        /// </summary>
        public event Action<HomeMiddleTab> TabRequested;
        public event Action SettingsRequested;

        public UITutorialTarget GetBottomTutorialTarget(HomeTutorialTarget tutorialTarget)
        {
            if (!Enum.IsDefined(typeof(HomeTutorialTarget), tutorialTarget)) throw new ArgumentOutOfRangeException(nameof(tutorialTarget));
            switch (tutorialTarget)
            {
                case HomeTutorialTarget.Map: return mapTutorialTarget;
                case HomeTutorialTarget.Shop: return shopTutorialTarget;
                default: throw new ArgumentException("This tutorial target is not in the Home bottom navigation.", nameof(tutorialTarget));
            }
        }

        public void InitializeSafeAreaContent(Canvas uiCanvas)
        {
            foreach (SafeAreaContentFitter safeAreaContentFitter in safeAreaContentFitters)
                safeAreaContentFitter.InitializeSafeAreaContent(uiCanvas);
        }

        private void Awake()
        {
            mapButton.onClick.AddListener(HandleMapClicked);
            shopButton.onClick.AddListener(HandleShopClicked);
            settingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        private void OnDestroy()
        {
            mapButton.onClick.RemoveListener(HandleMapClicked);
            shopButton.onClick.RemoveListener(HandleShopClicked);
            settingsButton.onClick.RemoveListener(HandleSettingsClicked);
        }

        private void HandleMapClicked()
        {
            TabRequested?.Invoke(HomeMiddleTab.Map);
        }

        private void HandleShopClicked()
        {
            TabRequested?.Invoke(HomeMiddleTab.Shop);
        }

        private void HandleSettingsClicked()
        {
            SettingsRequested?.Invoke();
        }
    }
}
