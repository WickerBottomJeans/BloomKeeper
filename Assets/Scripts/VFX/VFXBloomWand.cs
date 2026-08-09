using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.VFX
{
    public sealed class VFXBloomWand : MonoBehaviour
    {
        [SerializeField] private Transform wandPivot;
        [SerializeField] private Transform contactPoint;
        [SerializeField] private SpriteRenderer wandRenderer;
        [SerializeField] private ParticleSystem contactParticles;
        [SerializeField] private AudioCue contactCue;
        [SerializeField, Min(0f)] private float wandLengthInTiles = 1.5f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float travelDuration = 0.35f;
        [SerializeField, Min(0f)] private float rotationDuration = 0.12f;

        [Header("Motion")]
        [SerializeField, Min(1f)] private float entryScaleMultiplier = 3f;
        [SerializeField] private float bonkRotationDegrees = 90f;

        private Vector3 defaultRootLocalPosition;
        private Quaternion defaultRootLocalRotation;
        private Vector3 defaultRootLocalScale;
        private Quaternion defaultPivotLocalRotation;
        private Vector3 entryPosition;
        private Vector3 impactPosition;
        private Vector3 curveControlPosition;
        private Vector3 entryScale;
        private float entrySide;
        private bool isActive;
        private bool isPrepared;
        private bool hasFired;

        /// <summary>
        /// Keep every pooled appearance starting from the prefab's intended pose.
        /// </summary>
        private void Awake()
        {
            defaultRootLocalPosition = transform.localPosition;
            defaultRootLocalRotation = transform.localRotation;
            defaultRootLocalScale = transform.localScale;
            defaultPivotLocalRotation = wandPivot.localRotation;
        }

        /// <summary>
        /// Bring the wand from the player's corner into position without bonking yet.
        /// </summary>
        public async UniTask Prepare(Vector2 targetWorldPosition, Rect screenWorldBounds, float tileSize)
        {
            // Keep broken setup from leaving a pooled wand halfway active.
            if (tileSize <= 0f) throw new ArgumentOutOfRangeException(nameof(tileSize), tileSize, "Bloom Wand requires a positive tile size.");
            if (wandLengthInTiles <= 0f) throw new InvalidOperationException("Bloom Wand length in tiles must be positive.");
            if (isActive) throw new InvalidOperationException("Bloom Wand VFX is already active.");

            // Match the wand to the board and approach from the target's side.
            isActive = true;
            entrySide = targetWorldPosition.x < screenWorldBounds.center.x ? -1f : 1f;
            float targetZ = transform.position.z;
            Vector2 spriteSize = wandRenderer.sprite.bounds.size;
            float spriteLength = Mathf.Max(spriteSize.x, spriteSize.y);
            if (spriteLength <= 0f) throw new InvalidOperationException("Bloom Wand sprite must have positive bounds.");
            float scale = tileSize * wandLengthInTiles / spriteLength;
            Vector3 normalScale = new Vector3(scale, scale, defaultRootLocalScale.z);

            // Find where the wand must stop so the bonk lands exactly on the target.
            transform.localRotation = defaultRootLocalRotation;
            transform.localScale = normalScale;
            transform.position = new Vector3(targetWorldPosition.x, targetWorldPosition.y, targetZ);
            wandPivot.localRotation = GetImpactRotation();
            impactPosition = transform.position - (contactPoint.position - transform.position);
            transform.position = impactPosition;
            wandPivot.localRotation = defaultPivotLocalRotation;

            // Start large at the bottom corner so the wand feels launched by the player.
            entryScale = normalScale * entryScaleMultiplier;
            transform.localScale = entryScale;
            float entryX = entrySide < 0f ? screenWorldBounds.xMin : screenWorldBounds.xMax;
            float entryY = screenWorldBounds.yMin;
            entryPosition = new Vector3(entryX, entryY, impactPosition.z);

            // Keep the enlarged wand fully outside the screen at both ends of its flight.
            transform.position = entryPosition;
            entryPosition.y -= wandRenderer.bounds.max.y - screenWorldBounds.yMin;

            // Glide into place on a light curve while settling to board scale.
            transform.position = entryPosition;
            curveControlPosition = new Vector3(Mathf.Lerp(entryPosition.x, impactPosition.x, 0.35f), Mathf.Lerp(entryPosition.y, impactPosition.y, 0.5f), impactPosition.z);
            Sequence entrance = DOTween.Sequence();
            entrance.Join(DOVirtual.Float(0f, 1f, travelDuration, progress =>
            {
                float inverseProgress = 1f - progress;
                transform.position = inverseProgress * inverseProgress * entryPosition + 2f * inverseProgress * progress * curveControlPosition + progress * progress * impactPosition;
            }).SetEase(Ease.OutCubic));
            entrance.Join(transform.DOScale(normalScale, travelDuration).SetEase(Ease.OutBack));
            await entrance.SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
            isPrepared = true;
        }

        /// <summary>
        /// Swing the wand down and stop at the moment of contact.
        /// </summary>
        public async UniTask Fire()
        {
            if (!isPrepared || hasFired) throw new InvalidOperationException("Bloom Wand VFX must be prepared before firing.");

            await wandPivot.DOLocalRotate(GetImpactRotation().eulerAngles, rotationDuration).SetEase(Ease.InBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
            hasFired = true;
        }

        /// <summary>
        /// Sell the impact, retreat to the player's corner, and finish lingering particles.
        /// </summary>
        public async UniTask Finish()
        {
            if (!hasFired) throw new InvalidOperationException("Bloom Wand VFX must fire before finishing.");

            // Make the bonk feel immediate before the wand recoils.
            contactParticles.Play(true);
            AudioService.Instance.PlaySfx(contactCue);

            // Pull back along the same curve while returning to the player's perspective.
            Sequence retreat = DOTween.Sequence();
            retreat.Join(wandPivot.DOLocalRotate(defaultPivotLocalRotation.eulerAngles, rotationDuration).SetEase(Ease.OutBack));
            retreat.Join(DOVirtual.Float(0f, 1f, travelDuration, progress =>
            {
                float inverseProgress = 1f - progress;
                transform.position = inverseProgress * inverseProgress * impactPosition + 2f * inverseProgress * progress * curveControlPosition + progress * progress * entryPosition;
            }).SetEase(Ease.InCubic));
            retreat.Join(transform.DOScale(entryScale, travelDuration).SetEase(Ease.InCubic));
            await retreat.SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());

            // Let the world-space impact finish after the wand has left.
            wandRenderer.enabled = false;
            await UniTask.WaitUntil(() => !contactParticles.IsAlive(true), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// Get the wand pivot's bonk orientation for the current entry side.
        /// </summary>
        private Quaternion GetImpactRotation()
        {
            return defaultPivotLocalRotation * Quaternion.Euler(0f, 0f, bonkRotationDegrees * entrySide);
        }

        /// <summary>
        /// Restore a clean prefab state for the next pooled appearance.
        /// </summary>
        public void ResetForPool()
        {
            transform.DOKill();
            wandPivot.DOKill();
            contactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.localPosition = defaultRootLocalPosition;
            transform.localRotation = defaultRootLocalRotation;
            transform.localScale = defaultRootLocalScale;
            wandPivot.localRotation = defaultPivotLocalRotation;
            wandRenderer.enabled = true;
            entryPosition = default;
            impactPosition = default;
            curveControlPosition = default;
            entryScale = default;
            entrySide = 0f;
            isActive = false;
            isPrepared = false;
            hasFired = false;
        }
    }
}
