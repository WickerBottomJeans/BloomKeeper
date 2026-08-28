using System;
using DefaultNamespace.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ChapterTopperView : MonoBehaviour
    {
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text livesTimerText;
        [SerializeField] private TMP_Text unlimitedLivesTimerText;
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
            switch (lives.DisplayState)
            {
                case PlayerLivesDisplayState.Normal:
                    livesText.gameObject.SetActive(true);
                    livesText.text = $"{lives.DisplayedLives}/{lives.MaximumLives}";
                    livesTimerText.gameObject.SetActive(lives.RegenerationTimeRemaining.HasValue);
                    unlimitedLivesTimerText.gameObject.SetActive(false);
                    if (!lives.RegenerationTimeRemaining.HasValue) return;

                    livesTimerText.text = TimeDisplayFormatter.FormatCountdown(lives.RegenerationTimeRemaining.Value);
                    break;
                case PlayerLivesDisplayState.Unlimited:
                    livesText.gameObject.SetActive(false);
                    livesTimerText.gameObject.SetActive(false);
                    unlimitedLivesTimerText.gameObject.SetActive(true);
                    unlimitedLivesTimerText.text = $"<size=50%>Unlimited</size>\n{TimeDisplayFormatter.FormatCountdown(lives.UnlimitedLivesTimeRemaining.Value)}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lives.DisplayState), lives.DisplayState, "Unsupported player lives display state.");
            }
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
