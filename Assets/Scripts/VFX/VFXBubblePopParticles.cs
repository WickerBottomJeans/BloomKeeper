using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public sealed class VFXBubblePopParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private AudioCue popCue;

        private Vector3 defaultScale;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public void Configure(float tileSize, float inflatedScaleMultiplier)
        {
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Bubble burst VFX requires a positive tile size.");
            if (inflatedScaleMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(inflatedScaleMultiplier), inflatedScaleMultiplier, "Bubble burst VFX requires a positive inflated scale multiplier.");

            transform.localScale = defaultScale * tileSize * inflatedScaleMultiplier;
        }

        public async UniTask Play()
        {
            particles.Play(true);
            AudioService.Instance.PlaySfx(popCue);
            await UniTask.WaitUntil(() => !particles.IsAlive(true), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public void ResetForPool()
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.localScale = defaultScale;
        }
    }
}
