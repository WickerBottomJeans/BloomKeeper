using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameBootstrapper : MonoBehaviour
    {
        private void Start()
        {
            UIManager.Instance.ShowLevelSelect();
        }
    }
}