using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class ChapterTopperView : MonoBehaviour
    {
        [SerializeField] private TMP_Text livesText;
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

        public void DisplayLives(int value)
        {
            livesText.text = value.ToString();
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
