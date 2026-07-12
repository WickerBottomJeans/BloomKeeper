using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Auth screen view that displays account entry choices
    /// </summary>
    public class UIAuthScreen : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI playButtonLabel;

        public event Action PlayRequested;

        private void Awake()
        {
            playButton.onClick.AddListener(HandlePlayClicked);
        }

        public void Display(string playButtonText)
        {
            playButtonLabel.text = playButtonText;
        }

        private void HandlePlayClicked()
        {
            PlayRequested?.Invoke();
        }
    }
}
