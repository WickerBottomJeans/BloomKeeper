using Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private Toggle testerTogglePrefab;
        private Toggle testerToggleInstance;

        public void ShowTesterToggle()
        {
#if UNITY_EDITOR
            return;
#else
            if (testerToggleInstance == null)
            {
                testerToggleInstance = Instantiate(testerTogglePrefab, uiRoot);
                testerToggleInstance.onValueChanged.AddListener(active => GlobalState.SetAdminMode(active));
            }

            testerToggleInstance.SetIsOnWithoutNotify(GlobalState.IsAdminMode);
            testerToggleInstance.gameObject.SetActive(true);
#endif
        }

        public void HideTesterToggle()
        {
            if (testerToggleInstance == null) return;
            testerToggleInstance.gameObject.SetActive(false);
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tKey.wasPressedThisFrame) return;

            GlobalState.SetAdminMode(!GlobalState.IsAdminMode);
            if (testerToggleInstance != null)
                testerToggleInstance.SetIsOnWithoutNotify(GlobalState.IsAdminMode);
        }
#endif
    }
}
