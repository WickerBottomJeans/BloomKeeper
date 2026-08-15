using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIWinScreen : MonoBehaviour
    {
        [SerializeField] private Button homeButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private RawImage levelResultBackground;
        [SerializeField] private StarBoard starBoard;
        [SerializeField] private UIGiftBox giftBoxTemplate;
        [SerializeField] private RectTransform giftRoot;
        [SerializeField] private RewardPresentationConfig rewardPresentationConfig;
        [SerializeField] private UIPopupEntranceAnimator entranceAnimator;
        [SerializeField] private GameObject[] entranceVfx = Array.Empty<GameObject>();
        [SerializeField, Min(0f)] private float entranceVfxActivationSpan;
        [SerializeField] private float starRevealDuration = 0.6f;
        [SerializeField] private AudioCue winCue;

        private int pendingStarCount;
        private readonly List<UIGiftBox> spawnedGiftBoxes = new List<UIGiftBox>();
        
        public event Action HomeRequested;
        public event Action NextRequested;

        private CancellationTokenSource entranceVfxCancellation;

        private void Awake()
        {
            homeButton.onClick.AddListener(OnHomeClick);
            nextButton.onClick.AddListener(OnNextClick);
            giftBoxTemplate.gameObject.SetActive(false);
            ValidateEntranceVfxInactive();
        }

        private void OnEnable()
        {
            AudioService.Instance.PlayStinger(winCue);
            entranceVfxCancellation = new CancellationTokenSource();
            OnEntranceDone(entranceVfxCancellation.Token).Forget();
        }

        private void OnDisable()
        {
            entranceVfxCancellation?.Cancel();
            entranceVfxCancellation?.Dispose();
            entranceVfxCancellation = null;
            SetEntranceVfxActive(false);
        }

        public void Display(Texture levelResultBackgroundTexture, int stars, int starCap, bool showNext, IReadOnlyList<string> completionRewardPresentationKeys)
        {
            levelResultBackground.texture = levelResultBackgroundTexture;
            starBoard.SetStarCap(starCap);
            pendingStarCount = stars;
            nextButton.gameObject.SetActive(showNext);
            DisplayGiftBoxes(stars, completionRewardPresentationKeys);
        }

        private void DisplayGiftBoxes(int giftBoxCount, IReadOnlyList<string> completionRewardPresentationKeys)
        {
            if (completionRewardPresentationKeys == null) throw new ArgumentNullException(nameof(completionRewardPresentationKeys));
            if (completionRewardPresentationKeys.Count > giftBoxCount) throw new ArgumentException("Completion rewards exceed the gift box count.", nameof(completionRewardPresentationKeys));

            ClearSpawnedGiftBoxes();
            var giftRewardSprites = new List<Sprite>(giftBoxCount);
            foreach (string completionRewardPresentationKey in completionRewardPresentationKeys) giftRewardSprites.Add(rewardPresentationConfig.GetRewardSprite(completionRewardPresentationKey));
            while (giftRewardSprites.Count < giftBoxCount) giftRewardSprites.Add(null);
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

        private async UniTask OnEntranceDone(CancellationToken cancellationToken)
        {
            SetEntranceVfxActive(false);

            try
            {
                await UniTask.Yield(cancellationToken);
                await entranceAnimator.WaitForEntrance().AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            starBoard.DisplayAnimated(pendingStarCount, starRevealDuration);
            await PlayEntranceVfx(cancellationToken);
        }

        private async UniTask PlayEntranceVfx(CancellationToken cancellationToken)
        {
            if (entranceVfx.Length == 0) return;

            float activationGap = entranceVfx.Length > 1 ? entranceVfxActivationSpan / (entranceVfx.Length - 1) : 0f;

            for (int index = 0; index < entranceVfx.Length; index++)
            {
                entranceVfx[index].SetActive(true);
                if (index < entranceVfx.Length - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(activationGap), true, cancellationToken: cancellationToken);
            }
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
