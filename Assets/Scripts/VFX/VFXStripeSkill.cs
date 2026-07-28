 using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public class VFXStripeSkill : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer negativeBeam;
        [SerializeField] private SpriteRenderer positiveBeam;
        [SerializeField] private SpriteRenderer sourceHalo;
        [SerializeField, Min(0f)] private float sourceHaloSizeRelativeToBeam = 1.2f;
        [SerializeField, Range(0f, 1f)] private float sourceHaloStartScaleRatio = 0.6f;
        [SerializeField, Min(1f)] private float sourceHaloPulseScaleMultiplier = 1.08f;
        [SerializeField, Min(0f)] private float sourceHaloPulseHalfDuration = 0.25f;

        private float beamWorldWidth;
        private Vector3 sourceHaloRestingScale;
        private Color sourceHaloRestingColor;
        private Tween sourceHaloPulse;

        public void Configure(float width)
        {
            beamWorldWidth = width;
            transform.localScale = Vector3.one * beamWorldWidth;
        }

        public async UniTask Prepare(float duration)
        {
            float negativeWidthScale = 1f / negativeBeam.sprite.bounds.size.x;
            float positiveWidthScale = 1f / positiveBeam.sprite.bounds.size.x;
            float sourceHaloScale = sourceHaloSizeRelativeToBeam / Mathf.Max(sourceHalo.sprite.bounds.size.x, sourceHalo.sprite.bounds.size.y);
            sourceHaloRestingScale = new Vector3(sourceHaloScale, sourceHaloScale, 1f);
            sourceHaloRestingColor = sourceHalo.color;

            negativeBeam.transform.localScale = new Vector3(negativeWidthScale, 0f, 1f);
            positiveBeam.transform.localScale = new Vector3(positiveWidthScale, 0f, 1f);
            sourceHalo.transform.localScale = sourceHaloRestingScale * sourceHaloStartScaleRatio;
            sourceHalo.color = new Color(sourceHaloRestingColor.r, sourceHaloRestingColor.g, sourceHaloRestingColor.b, 0f);

            await UniTask.WhenAll(
                sourceHalo.DOFade(sourceHaloRestingColor.a, duration).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()),
                sourceHalo.transform.DOScale(sourceHaloRestingScale, duration).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()));

            sourceHaloPulse = sourceHalo.transform.DOScale(sourceHaloRestingScale * sourceHaloPulseScaleMultiplier, sourceHaloPulseHalfDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public async UniTask Fire(Vector2 negativeEndWorld, Vector2 positiveEndWorld, float negativeTravelDuration, float positiveTravelDuration)
        {
            Vector2 sourceWorld = transform.position;
            Vector2 beamAxis = positiveEndWorld - negativeEndWorld;
            float beamRotation = Mathf.Atan2(beamAxis.y, beamAxis.x) * Mathf.Rad2Deg - 90f;
            float negativeWidthScale = 1f / negativeBeam.sprite.bounds.size.x;
            float positiveWidthScale = 1f / positiveBeam.sprite.bounds.size.x;
            float negativeLengthScale = Vector2.Distance(sourceWorld, negativeEndWorld) / beamWorldWidth / negativeBeam.sprite.bounds.size.y;
            float positiveLengthScale = Vector2.Distance(sourceWorld, positiveEndWorld) / beamWorldWidth / positiveBeam.sprite.bounds.size.y;

            negativeBeam.transform.position = sourceWorld;
            positiveBeam.transform.position = sourceWorld;
            negativeBeam.transform.rotation = Quaternion.Euler(0f, 0f, beamRotation + 180f);
            positiveBeam.transform.rotation = Quaternion.Euler(0f, 0f, beamRotation);
            negativeBeam.flipX = true;
            positiveBeam.flipX = false;
            negativeBeam.transform.localScale = new Vector3(negativeWidthScale, 0f, 1f);
            positiveBeam.transform.localScale = new Vector3(positiveWidthScale, 0f, 1f);

            await UniTask.WhenAll(
                negativeBeam.transform.DOScaleY(negativeLengthScale, negativeTravelDuration).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()),
                positiveBeam.transform.DOScaleY(positiveLengthScale, positiveTravelDuration).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()));
        }

        public async UniTask Finish(float duration)
        {
            sourceHaloPulse?.Kill();
            await UniTask.WhenAll(
                sourceHalo.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()),
                negativeBeam.transform.DOScaleX(0f, duration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()),
                positiveBeam.transform.DOScaleX(0f, duration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()));
        }
    }
}
    
