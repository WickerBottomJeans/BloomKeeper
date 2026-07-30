using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public sealed class VFXPrismaticBloomFinisher : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;

        private Vector3 defaultScale;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public void Configure(float tileSize)
        {
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Prismatic Bloom finisher requires a positive tile size.");

            transform.localScale = defaultScale * tileSize;
        }

        public async UniTask Play()
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
            await UniTask.WaitUntil(() => !particles.IsAlive(true), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public void ResetForPool()
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.localScale = defaultScale;
        }
    }
}
