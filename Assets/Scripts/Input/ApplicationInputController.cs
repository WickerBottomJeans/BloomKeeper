using DefaultNamespace.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DefaultNamespace
{
    public class ApplicationInputController : Singleton<ApplicationInputController>
    {
        [SerializeField] private InputActionReference uiAction;
        [SerializeField] private InputActionReference gameBoardAction;

        private bool uiInputActive;
        private bool gameBoardInputActive;
        private bool inputSuspended;

        public void SetUIInputActive(bool active)
        {
            uiInputActive = active;
            RefreshInputState();
        }

        public void SetGameBoardInputActive(bool active)
        {
            gameBoardInputActive = active;
            RefreshInputState();
        }

        public void SetInputSuspended(bool suspended)
        {
            inputSuspended = suspended;
            RefreshInputState();
        }

        private void OnEnable() => RefreshInputState();

        private void OnDisable()
        {
            uiAction.action.actionMap.Disable();
            gameBoardAction.action.actionMap.Disable();
        }

        private void RefreshInputState()
        {
            SetActionMapActive(uiAction.action.actionMap, uiInputActive && !inputSuspended);
            SetActionMapActive(gameBoardAction.action.actionMap, gameBoardInputActive && !inputSuspended);
        }

        private  void SetActionMapActive(InputActionMap actionMap, bool active)
        {
            if (active)
                actionMap.Enable();
            else
                actionMap.Disable();
        }
    }
}
