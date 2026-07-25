using System;
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
        [SerializeField] private StarBoard starBoard;
        [SerializeField] private UIPopupEntranceAnimator entranceAnimator;
        [SerializeField] private GameObject[] entranceVfx = Array.Empty<GameObject>();
        [SerializeField, Min(0f)] private float entranceVfxActivationSpan;
        [SerializeField] private float starRevealDuration = 0.6f;
        [SerializeField] private AudioCue winCue;

        private int pendingStarCount;
        
        public event Action HomeRequested;
        public event Action NextRequested;

        private CancellationTokenSource entranceVfxCancellation;

        private void Awake()
        {
            homeButton.onClick.AddListener(OnHomeClick);
            nextButton.onClick.AddListener(OnNextClick);
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

        public void Display(int stars, int starCap, bool showNext)
        {
            starBoard.Init(starCap);
            pendingStarCount = stars;
            nextButton.gameObject.SetActive(showNext);
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

            starBoard.DisplayStars(pendingStarCount, starRevealDuration);
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
