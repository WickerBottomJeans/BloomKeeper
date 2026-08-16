using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        // DialogManager is the sole intended caller and subscriber for this dialog view API.
        [SerializeField] private UIDialog dialogPrefab;
        private UIDialog dialogInstance;

        public event Action<int> DialogButtonClicked;

        public void PresentDialogView(string title, string message, IReadOnlyList<DialogOptionButton> options)
        {
            bool isNewDialogInstance = dialogInstance == null;
            GetPanel(ref dialogInstance, dialogPrefab, uiRoot);
            if (isNewDialogInstance)
            {
                dialogInstance.ButtonClicked += HandleDialogButtonClicked;
            }

            dialogInstance.Display(title, message, options);
            dialogInstance.Show();
        }

        public void DismissDialogView()
        {
            dialogInstance?.Hide();
        }

        public void SetDialogButtonsInteractable(bool interactable)
        {
            dialogInstance.SetButtonsInteractable(interactable);
        }

        private void HandleDialogButtonClicked(int buttonId)
        {
            DialogButtonClicked?.Invoke(buttonId);
        }
    }
}
