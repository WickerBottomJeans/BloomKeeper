using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Displays the startup background and account entry visuals.
    /// </summary>
    public class UIStartupScreen : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private CanvasGroup accountEntryVisualGroup;

        public event Action PlayRequested;

        private void Awake()
        {
            playButton.onClick.AddListener(HandlePlayClicked);
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
        }

        public void SetAccountEntryVisible(bool visible)
        {
            accountEntryVisualGroup.gameObject.SetActive(visible);
        }

        private void HandlePlayClicked()
        {
            PlayRequested?.Invoke();
        }
    }
}
