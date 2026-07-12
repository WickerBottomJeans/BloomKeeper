using System;
using System.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class BootFlow
    {
        public event Action BootCompleted;

        public async void Enter()
        {
            ConfigureFrameRate();
            await SpriteLoader.Instance.LoadAll();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.ShowTesterToggle();
#endif
            BootCompleted?.Invoke();
        }

        private static void ConfigureFrameRate()
        {
            // TODO: This is just temporary so i can test some stuff. MUST redo later
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
