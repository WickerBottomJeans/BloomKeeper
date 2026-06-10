using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameBootstrapper : MonoBehaviour
    {
        private async void Start()
        {
            await SpriteLoader.Instance.LoadAll();
            UIManager.Instance.ShowLevelSelect();
        }
    }
}