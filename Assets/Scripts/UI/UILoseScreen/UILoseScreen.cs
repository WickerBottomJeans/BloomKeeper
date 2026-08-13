using System;
using DefaultNamespace.Audio;
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
        [SerializeField] private RawImage levelResultBackground;
        [SerializeField] private AudioCue loseCue;

        public event Action RetryRequested;
        public event Action HomeRequested;

        private void Awake()
        {
            retryButton.onClick.AddListener(HandleRetryClicked);
            homeButton.onClick.AddListener(HandleHomeClicked);
        }

        private void OnEnable()
        {
            AudioService.Instance.PlayStinger(loseCue);
        }

        public void Display(Texture levelResultBackgroundTexture, string message)
        {
            levelResultBackground.texture = levelResultBackgroundTexture;
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
