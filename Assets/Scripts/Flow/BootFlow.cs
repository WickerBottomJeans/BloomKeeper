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
            ConfigureFrameRate();
            await addressableContentService.InitializeAsync();
            await ConfigManager.Instance.InitializeAsync();
            // TODO: Move the shared sprite atlases to remote Addressables.
            await SpriteLoader.Instance.LoadAll();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.ShowTesterToggle();
#endif
        }

        private  void ConfigureFrameRate()
        {
            // TODO: This is just temporary so i can test some stuff. MUST redo later
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
