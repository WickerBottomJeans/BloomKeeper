using Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private Toggle testerTogglePrefab;
        private Toggle testerToggleInstance;

        public void ShowTesterToggle()
        {
            if (testerToggleInstance != null)
            {
                testerToggleInstance.gameObject.SetActive(true);
                return;
            }
            testerToggleInstance = Instantiate(testerTogglePrefab, canvas.transform);
            testerToggleInstance.onValueChanged.AddListener(active => GlobalState.SetAdminMode(active));
        }

        public void HideTesterToggle()
        {
            if (testerToggleInstance == null) return;
            testerToggleInstance.gameObject.SetActive(false);
        }
    }
}