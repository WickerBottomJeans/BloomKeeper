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
        [SerializeField] private RectTransformEdgeBleed backgroundBleed;

        /// <summary>
        /// Home tab selected by the player.
        /// </summary>
        public event Action<HomeMiddleTab> TabRequested;
        public event Action SettingsRequested;

        public void SetBleedTarget(RectTransform targetRect)
        {
            backgroundBleed.SetTarget(targetRect);
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
