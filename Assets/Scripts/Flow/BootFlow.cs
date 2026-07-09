using System.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class BootFlow
    {
        public async Task Enter()
        {
            ConfigureFrameRate();
            await SpriteLoader.Instance.LoadAll();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.ShowTesterToggle();
#endif
        }

        private static void ConfigureFrameRate()
        {
            // TODO: This is just temporary so i can test some stuff. MUST redo later
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
