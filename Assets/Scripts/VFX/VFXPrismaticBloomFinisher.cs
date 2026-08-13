using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public class VFXPrismaticBloomFinisher : MonoBehaviour
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

        public async UniTask Play(CancellationToken cancellationToken)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
            await UniTask.WaitUntil(() => !particles.IsAlive(true), cancellationToken: cancellationToken);
        }

        public void ResetForPool()
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.localScale = defaultScale;
        }
    }
}
