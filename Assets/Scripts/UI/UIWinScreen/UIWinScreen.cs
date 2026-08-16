using System;
using System.Collections.Generic;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIWinScreen : UIPopup
    {
        [SerializeField] private Button homeButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private StarBoard starBoard;
        [SerializeField] private UIGiftBox giftBoxTemplate;
        [SerializeField] private RectTransform giftRoot;
        [SerializeField] private RewardPresentationConfig rewardPresentationConfig;
        [SerializeField] private GameObject[] entranceVfx = Array.Empty<GameObject>();
        [SerializeField] private float starRevealDuration = 0.6f;
        [SerializeField] private AudioCue winCue;

        private int pendingStarCount;
        private readonly List<UIGiftBox> spawnedGiftBoxes = new List<UIGiftBox>();
        
        public event Action HomeRequested;
        public event Action NextRequested;

        protected override void Awake()
        {
            base.Awake();
            homeButton.onClick.AddListener(OnHomeClick);
            nextButton.onClick.AddListener(OnNextClick);
            giftBoxTemplate.gameObject.SetActive(false);
            ValidateEntranceVfxInactive();
        }

        private void OnEnable()
        {
            AudioService.Instance.PlayStinger(winCue);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetEntranceVfxActive(false);
        }

        public void Display(int stars, int starCap, bool showNext, RewardDisplayData rewardDisplayData)
        {
            starBoard.SetStarCap(starCap);
            pendingStarCount = stars;
            nextButton.gameObject.SetActive(showNext);
            DisplayGiftBoxes(rewardDisplayData);
        }

        private void DisplayGiftBoxes(RewardDisplayData rewardDisplayData)
        {
            ClearSpawnedGiftBoxes();
            var giftRewardSprites = new List<Sprite>(rewardDisplayData.Amount);
            foreach (string completionRewardPresentationKey in rewardDisplayData.CompletionRewardPresentationKeys) giftRewardSprites.Add(rewardPresentationConfig.GetRewardSprite(completionRewardPresentationKey));
            while (giftRewardSprites.Count < rewardDisplayData.Amount) giftRewardSprites.Add(null);
            ShuffleGiftRewardSprites(giftRewardSprites);

            foreach (Sprite giftRewardSprite in giftRewardSprites)
            {
                UIGiftBox spawnedGiftBox = Instantiate(giftBoxTemplate, giftRoot);
                spawnedGiftBox.gameObject.SetActive(true);
                spawnedGiftBox.DisplayGiftBox(giftRewardSprite);
                spawnedGiftBoxes.Add(spawnedGiftBox);
            }
        }

        private void ShuffleGiftRewardSprites(List<Sprite> giftRewardSprites)
        {
            for (int rewardSpriteIndex = giftRewardSprites.Count - 1; rewardSpriteIndex > 0; rewardSpriteIndex--)
            {
                int swapRewardSpriteIndex = UnityEngine.Random.Range(0, rewardSpriteIndex + 1);
                (giftRewardSprites[rewardSpriteIndex], giftRewardSprites[swapRewardSpriteIndex]) = (giftRewardSprites[swapRewardSpriteIndex], giftRewardSprites[rewardSpriteIndex]);
            }
        }

        private void ClearSpawnedGiftBoxes()
        {
            foreach (UIGiftBox spawnedGiftBox in spawnedGiftBoxes)
            {
                spawnedGiftBox.gameObject.SetActive(false);
                Destroy(spawnedGiftBox.gameObject);
            }

            spawnedGiftBoxes.Clear();
        }

        protected override void HandlePopupEntranceCompleted()
        {
            starBoard.DisplayAnimated(pendingStarCount, starRevealDuration);
            PlayEntranceVfx();
        }

        private void PlayEntranceVfx()
        {
            SetEntranceVfxActive(true);
        }

        private void ValidateEntranceVfxInactive()
        {
            foreach (GameObject entranceVfxObject in entranceVfx)
            {
                if (entranceVfxObject.activeSelf)
                    Debug.LogError($"{nameof(UIWinScreen)} entrance VFX '{entranceVfxObject.name}' must be inactive in the prefab before the win screen entrance plays.", entranceVfxObject);
            }
        }

        private void SetEntranceVfxActive(bool isActive)
        {
            foreach (GameObject entranceVfxObject in entranceVfx)
                entranceVfxObject.SetActive(isActive);
        }

        private void OnHomeClick()
        {
            HomeRequested?.Invoke();
        }

        private void OnNextClick()
        {
            NextRequested?.Invoke();
        }
    }
}
