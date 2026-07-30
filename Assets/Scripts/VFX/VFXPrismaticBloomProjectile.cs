using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace.VFX
{
    public sealed class VFXPrismaticBloomProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer projectileRenderer;
        [SerializeField] private Sprite[] projectileSprites;
        [SerializeField] private ParticleSystem flyingParticle;
        [SerializeField] private ParticleSystem enderParticle;
        [SerializeField] private Vector2 localHeadDirection = Vector2.up;

        private Vector3 defaultScale;

        private void Awake()
        {
            if (localHeadDirection.sqrMagnitude <= Mathf.Epsilon)
                throw new InvalidOperationException("Prismatic Bloom projectile requires a non-zero local head direction.");

            defaultScale = transform.localScale;
        }

        public void Configure(float tileSize)
        {
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Prismatic Bloom projectile requires a positive tile size.");

            transform.localScale = defaultScale * tileSize;
        }

        public UniTask Shoot(Vector3 origin, Vector3 target, float duration)
        {
            enderParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flyingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            projectileRenderer.sprite = projectileSprites[Random.Range(0, projectileSprites.Length)];
            projectileRenderer.enabled = true;
            transform.position = origin;
            Vector3 direction = target - origin;
            float angle = Vector2.SignedAngle(localHeadDirection, direction);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            flyingParticle.Play(true);
            return transform.DOMove(target, duration).SetEase(Ease.OutQuad).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        public async UniTask Finish()
        {
            projectileRenderer.enabled = false;
            flyingParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            enderParticle.Play(true);
            await UniTask.WaitUntil(() => !flyingParticle.IsAlive(true) && !enderParticle.IsAlive(true), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public void ResetForPool()
        {
            transform.DOKill();
            flyingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            enderParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            projectileRenderer.enabled = false;
            transform.localScale = defaultScale;
        }
    }
}
