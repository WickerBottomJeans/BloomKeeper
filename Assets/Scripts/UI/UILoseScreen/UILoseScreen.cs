using System;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UILoseScreen : UIPopup
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private AudioCue loseCue;

        public event Action RetryRequested;
        public event Action HomeRequested;

        protected override void Awake()
        {
            base.Awake();
            retryButton.onClick.AddListener(HandleRetryClicked);
            homeButton.onClick.AddListener(HandleHomeClicked);
        }

        private void OnEnable()
        {
            AudioService.Instance.PlayStinger(loseCue);
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
