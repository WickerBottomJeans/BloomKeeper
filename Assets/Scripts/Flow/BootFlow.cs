using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class BootFlow
    {
        private readonly AddressableContentService addressableContentService;

        public BootFlow(AddressableContentService addressableContentService)
        {
            this.addressableContentService = addressableContentService ?? throw new ArgumentNullException(nameof(addressableContentService));
        }

        public async UniTask Run()
        {
            ConfigureDeviceFrameRate();
            UIManager.Instance.ShowStartupScreen();
            UIManager.Instance.SetStartupAccountEntryVisible(false);
            try
            {
                await UIManager.Instance.ShowLoading();
                await addressableContentService.InitializeAsync();
                await ConfigManager.Instance.InitializeAsync();
                await SpriteCatalogProvider.Instance.LoadAsync();
            }
            finally
            {
                await UIManager.Instance.HideLoading();
            }

            UIManager.Instance.SetStartupAccountEntryVisible(true);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.ShowTesterToggle();
#endif
        }

        private void ConfigureDeviceFrameRate()
        {
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        }
    }
}
