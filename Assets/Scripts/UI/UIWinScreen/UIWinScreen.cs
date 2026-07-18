using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private float starRevealDuration = 0.6f;

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

            SetEntranceVfxActive(true);
            starBoard.DisplayStars(pendingStarCount, starRevealDuration);

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
