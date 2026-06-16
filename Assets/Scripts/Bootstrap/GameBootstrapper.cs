using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameBootstrapper : MonoBehaviour
    {
        private async void Start()
        {
            await SpriteLoader.Instance.LoadAll();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.ShowTesterToggle();
#endif

            UIManager.Instance.ShowLevelSelect();
        }
    }
}