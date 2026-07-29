using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public sealed class VFXBubble : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bubbleRenderer;
        [SerializeField] private ParticleSystem enderParticle;
        [SerializeField] private AudioCue popCue;

        private Vector3 defaultScale;
        private Vector3 configuredScale;
        private Color defaultColor;

        private void Awake()
        {
            defaultScale = transform.localScale;
            configuredScale = defaultScale;
            defaultColor = bubbleRenderer.color;
        }

        public void Configure(float tileSize)
        {
            if (tileSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Bubble VFX requires a positive tile size.");

            Vector2 spriteSize = bubbleRenderer.sprite.bounds.size;
            float authoredWidth = Mathf.Abs(spriteSize.x * defaultScale.x);
            float authoredHeight = Mathf.Abs(spriteSize.y * defaultScale.y);
            float authoredLongestSide = Mathf.Max(authoredWidth, authoredHeight);
            if (authoredLongestSide <= 0f)
                throw new InvalidOperationException("Bubble VFX sprite must have positive bounds.");

            configuredScale = defaultScale * (tileSize / authoredLongestSide);
            ResetVisualState();
        }

        public UniTask Shoot(Vector3 origin, Vector3 target, float duration)
        {
            ResetVisualState();
            transform.position = origin;
            return transform.DOMove(target, duration).SetEase(Ease.OutQuad).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        public void Pop()
        {
            bubbleRenderer.enabled = false;
            enderParticle.Play(true);
            AudioService.Instance.PlaySfx(popCue);
        }

        public UniTask WaitForEnderParticles()
        {
            return UniTask.WaitUntil(() => !enderParticle.IsAlive(true), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public void ResetForPool()
        {
            enderParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            configuredScale = defaultScale;
            ResetVisualState();
        }

        private void ResetVisualState()
        {
            transform.DOKill();
            bubbleRenderer.DOKill();
            transform.localScale = configuredScale;
            bubbleRenderer.color = defaultColor;
            bubbleRenderer.enabled = true;
        }
    }
}
