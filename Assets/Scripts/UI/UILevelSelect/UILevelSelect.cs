using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UILevelSelect : MonoBehaviour
    {
        [SerializeField] private ScrollMapBGController bgController;
        [SerializeField] private ScrollMapController mapController;

        private void Start()
        {
            bgController.Init();
            mapController.Init();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            mapController.Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}