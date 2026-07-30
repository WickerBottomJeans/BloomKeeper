using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public sealed class VFXStripeHalo : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceHalo;
        [SerializeField] private AudioCue skillCue;
        [SerializeField, Min(0f)] private float sourceHaloSizeRelativeToBeam = 1.2f;
        [SerializeField, Range(0f, 1f)] private float sourceHaloStartScaleRatio = 0.6f;
        [SerializeField, Min(1f)] private float sourceHaloPulseScaleMultiplier = 1.08f;
        [SerializeField, Min(0f)] private float sourceHaloPulseHalfDuration = 0.25f;

        private Vector3 defaultScale;
        private Vector3 defaultHaloScale;
        private Color defaultHaloColor;
        private Vector3 sourceHaloRestingScale;
        private Tween sourceHaloPulse;

        private void Awake()
        {
            defaultScale = transform.localScale;
            defaultHaloScale = sourceHalo.transform.localScale;
            defaultHaloColor = sourceHalo.color;
        }

        public void Configure(float tileSize)
        {
            transform.localScale = defaultScale * tileSize;
            float sourceHaloScale = sourceHaloSizeRelativeToBeam / Mathf.Max(sourceHalo.sprite.bounds.size.x, sourceHalo.sprite.bounds.size.y);
            sourceHaloRestingScale = new Vector3(sourceHaloScale, sourceHaloScale, 1f);
        }

        public async UniTask Prepare(float duration)
        {
            AudioService.Instance.PlaySfx(skillCue);
            sourceHalo.transform.localScale = sourceHaloRestingScale * sourceHaloStartScaleRatio;
            sourceHalo.color = new Color(defaultHaloColor.r, defaultHaloColor.g, defaultHaloColor.b, 0f);

            await UniTask.WhenAll(
                sourceHalo.DOFade(defaultHaloColor.a, duration).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()),
                sourceHalo.transform.DOScale(sourceHaloRestingScale, duration).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()));

            sourceHaloPulse = sourceHalo.transform.DOScale(sourceHaloRestingScale * sourceHaloPulseScaleMultiplier, sourceHaloPulseHalfDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public async UniTask Finish(float duration)
        {
            sourceHaloPulse?.Kill();
            await sourceHalo.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        public void ResetForPool()
        {
            sourceHaloPulse?.Kill();
            sourceHaloPulse = null;
            sourceHalo.DOKill();
            sourceHalo.transform.DOKill();
            transform.localScale = defaultScale;
            sourceHalo.transform.localScale = defaultHaloScale;
            sourceHalo.color = defaultHaloColor;
        }
    }
}
