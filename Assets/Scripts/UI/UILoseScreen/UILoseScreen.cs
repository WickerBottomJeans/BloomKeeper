using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UILoseScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        public event Action RetryRequested;
        public event Action HomeRequested;

        private void Awake()
        {
            retryButton.onClick.AddListener(HandleRetryClicked);
            homeButton.onClick.AddListener(HandleHomeClicked);
        }

        public void Display(string message)
        {
            if (label == null) return;
            label.text = message;
        }

        private void HandleRetryClicked()
        {
            RetryRequested?.Invoke();
        }

        private void HandleHomeClicked()
        {
            HomeRequested?.Invoke();
        }
    }
}
