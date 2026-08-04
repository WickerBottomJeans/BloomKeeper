using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class ChapterBottomView : MonoBehaviour
    {
        [SerializeField] private Button mapButton;
        [SerializeField] private Button socialButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        public event Action<HomeMiddleTab> TabRequested;
        public event Action SettingsRequested;

        private void Awake()
        {
            mapButton.onClick.AddListener(HandleMapClicked);
            socialButton.onClick.AddListener(HandleSocialClicked);
            shopButton.onClick.AddListener(HandleShopClicked);
            settingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        private void OnDestroy()
        {
            mapButton.onClick.RemoveListener(HandleMapClicked);
            socialButton.onClick.RemoveListener(HandleSocialClicked);
            shopButton.onClick.RemoveListener(HandleShopClicked);
            settingsButton.onClick.RemoveListener(HandleSettingsClicked);
        }

        private void HandleMapClicked()
        {
            TabRequested?.Invoke(HomeMiddleTab.Map);
        }

        private void HandleSocialClicked()
        {
            TabRequested?.Invoke(HomeMiddleTab.Friends);
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
