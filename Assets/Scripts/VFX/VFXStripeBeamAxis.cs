using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public sealed class VFXStripeBeamAxis : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer negativeBeam;
        [SerializeField] private SpriteRenderer positiveBeam;

        private float beamWorldWidth;
        private Vector3 defaultScale;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public void Configure(float tileSize)
        {
            beamWorldWidth = tileSize;
            transform.localScale = defaultScale * tileSize;

            float negativeWidthScale = 1f / negativeBeam.sprite.bounds.size.x;
            float positiveWidthScale = 1f / positiveBeam.sprite.bounds.size.x;
            negativeBeam.transform.localScale = new Vector3(negativeWidthScale, 0f, 1f);
            positiveBeam.transform.localScale = new Vector3(positiveWidthScale, 0f, 1f);
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
            await UniTask.WhenAll(
                negativeBeam.transform.DOScaleX(0f, duration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()),
                positiveBeam.transform.DOScaleX(0f, duration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy()));
        }

        public void ResetForPool()
        {
            negativeBeam.transform.DOKill();
            positiveBeam.transform.DOKill();
            transform.localScale = defaultScale;
            negativeBeam.transform.localPosition = Vector3.zero;
            positiveBeam.transform.localPosition = Vector3.zero;
            negativeBeam.transform.localRotation = Quaternion.identity;
            positiveBeam.transform.localRotation = Quaternion.identity;
            negativeBeam.transform.localScale = Vector3.one;
            positiveBeam.transform.localScale = Vector3.one;
        }
    }
}
