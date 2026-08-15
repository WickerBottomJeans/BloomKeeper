using System.Collections.Generic;
using DefaultNamespace.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIGiftBox : MonoBehaviour
    {
        [SerializeField] private Button giftBoxButton;
        [SerializeField] private Image giftBoxImage;
        [SerializeField] private List<Sprite> giftBoxSprites = new List<Sprite>();
        [SerializeField] private Image rewardImage;
        [SerializeField] private AudioCue successfulGiftOpeningCue;
        [SerializeField] private AudioCue failedGiftOpeningCue;

        private void Awake()
        {
            giftBoxButton.onClick.AddListener(HandleGiftBoxClicked);
        }

        private void OnDestroy()
        {
            giftBoxButton.onClick.RemoveListener(HandleGiftBoxClicked);
        }

        public void DisplayGiftBox(Sprite rewardSprite)
        {
            giftBoxImage.sprite = giftBoxSprites[UnityEngine.Random.Range(0, giftBoxSprites.Count)];
            rewardImage.sprite = rewardSprite;
            giftBoxImage.gameObject.SetActive(true);
            rewardImage.gameObject.SetActive(true);
            SetGiftBoxVisualState(true, false);
            giftBoxButton.interactable = true;
        }

        private void HandleGiftBoxClicked()
        {
            giftBoxButton.interactable = false;
            bool hasReward = rewardImage.sprite != null;
            RevealGiftBoxReward(hasReward);
        }

        private void RevealGiftBoxReward(bool hasReward)
        {
            SetGiftBoxVisualState(false, hasReward);
            AudioService.Instance.PlaySfx(hasReward ? successfulGiftOpeningCue : failedGiftOpeningCue);
        }

        private void SetGiftBoxVisualState(bool showGiftBox, bool showReward)
        {
            giftBoxImage.enabled = showGiftBox;
            rewardImage.enabled = showReward;
        }

    }
}
