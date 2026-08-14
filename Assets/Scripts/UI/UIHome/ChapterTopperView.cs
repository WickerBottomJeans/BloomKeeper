using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ChapterTopperView : MonoBehaviour
    {
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text livesTimerText;
        [SerializeField] private TMP_Text currencyText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Button addLifeButton;
        [SerializeField] private Button addCurrencyButton;
        [SerializeField] private RectTransformEdgeBleed backgroundBleed;

        public event Action AddLifeRequested;
        public event Action AddCurrencyRequested;

        private void Awake()
        {
            addLifeButton.onClick.AddListener(HandleAddLifeClicked);
            addCurrencyButton.onClick.AddListener(HandleAddCurrencyClicked);
        }

        private void OnDestroy()
        {
            addLifeButton.onClick.RemoveListener(HandleAddLifeClicked);
            addCurrencyButton.onClick.RemoveListener(HandleAddCurrencyClicked);
        }

        public void DisplayLives(PlayerLivesViewData lives)
        {
            livesText.text = $"{lives.DisplayedLives}/{lives.MaximumLives}";
            livesTimerText.gameObject.SetActive(lives.RegenerationTimeRemaining.HasValue);
            if (!lives.RegenerationTimeRemaining.HasValue) return;

            int remainingSeconds = Math.Max(1, (int)Math.Ceiling(lives.RegenerationTimeRemaining.Value.TotalSeconds));
            livesTimerText.text = $"{remainingSeconds / 60}:{remainingSeconds % 60:00}";
        }

        public void DisplayCurrency(int value)
        {
            currencyText.text = value.ToString();
        }

        public void DisplayAvatar(Sprite avatar)
        {
            avatarImage.sprite = avatar;
        }

        public void SetBleedTarget(RectTransform targetRect)
        {
            backgroundBleed.SetTarget(targetRect);
        }

        private void HandleAddLifeClicked()
        {
            AddLifeRequested?.Invoke();
        }

        private void HandleAddCurrencyClicked()
        {
            AddCurrencyRequested?.Invoke();
        }
    }
}
