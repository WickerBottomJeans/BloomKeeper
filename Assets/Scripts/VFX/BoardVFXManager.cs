using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.Pool;

namespace DefaultNamespace.VFX
{
    //TODO: we need to care for the case skill A destroy petal skill B. we need to leave B alone so it can run its own VFX next cascade
    public sealed class BoardVFXManager : MonoBehaviour
    {
        [SerializeField] private MutationLaserView mutationLaserPrefab;
        [SerializeField] private ParticleSystem mutationLaserOriginPrefab;
        [SerializeField] private VFXStripeSkill stripedSkillPrefab;
        [SerializeField] private VFXButterflySkill butterflySkillPrefab;
        [SerializeField] private VFXBubble bubblePrefab;
        [SerializeField] private VFXBubblePopParticles bubblePopParticlesPrefab;
        [SerializeField] private Transform boardVFXRoot;
        [SerializeField] private float mutationLaserWidthRatio = 0.12f;

        private ObjectPool<MutationLaserView> mutationLaserPool;
        private ObjectPool<ParticleSystem> mutationLaserOriginPool;
        private ObjectPool<VFXStripeSkill> stripedSkillPool;
        private ObjectPool<VFXButterflySkill> butterflySkillPool;
        private ObjectPool<VFXBubble> bubblePool;
        private ObjectPool<VFXBubblePopParticles> bubblePopParticlesPool;
        private BoardLayout layout;

        public void Init(BoardLayout boardLayout)
        {
            if (mutationLaserPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a MutationLaserView prefab.");
            if (mutationLaserOriginPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a mutation laser origin ParticleSystem prefab.");
            if (stripedSkillPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a striped skill prefab.");
            if (bubblePrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a Bubble projectile prefab.");
            if (bubblePopParticlesPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a Bubble pop-particles prefab.");

            layout = boardLayout ?? throw new ArgumentNullException(nameof(boardLayout));
            Transform root = boardVFXRoot != null ? boardVFXRoot : transform;

            mutationLaserPool = new ObjectPool<MutationLaserView>(
                createFunc: () => Instantiate(mutationLaserPrefab, root),
                actionOnGet: laser => laser.gameObject.SetActive(true),
                actionOnRelease: laser => laser.gameObject.SetActive(false),
                actionOnDestroy: laser => Destroy(laser.gameObject)
            );

            mutationLaserOriginPool = new ObjectPool<ParticleSystem>(
                createFunc: () => Instantiate(mutationLaserOriginPrefab, root),
                actionOnGet: particles => particles.gameObject.SetActive(true),
                actionOnRelease: particles => particles.gameObject.SetActive(false),
                actionOnDestroy: particles => Destroy(particles.gameObject)
            );

            stripedSkillPool = new ObjectPool<VFXStripeSkill>(
                createFunc: () => Instantiate(stripedSkillPrefab, root),
                actionOnGet: stripe => stripe.gameObject.SetActive(true),
                actionOnRelease: stripe => stripe.gameObject.SetActive(false),
                actionOnDestroy: stripe => Destroy(stripe.gameObject)
            );

            butterflySkillPool = new ObjectPool<VFXButterflySkill>(
                createFunc: () => Instantiate(butterflySkillPrefab, root),
                actionOnGet: butterfly => butterfly.gameObject.SetActive(true),
                actionOnRelease: butterfly => butterfly.gameObject.SetActive(false),
                actionOnDestroy: butterfly => Destroy(butterfly.gameObject)
            );

            bubblePool = new ObjectPool<VFXBubble>(
                createFunc: () => Instantiate(bubblePrefab, root),
                actionOnGet: bubble =>
                {
                    bubble.gameObject.SetActive(true);
                    bubble.ResetForPool();
                },
                actionOnRelease: bubble =>
                {
                    bubble.ResetForPool();
                    bubble.gameObject.SetActive(false);
                },
                actionOnDestroy: bubble => Destroy(bubble.gameObject)
            );

            bubblePopParticlesPool = new ObjectPool<VFXBubblePopParticles>(
                createFunc: () => Instantiate(bubblePopParticlesPrefab, root),
                actionOnGet: particles =>
                {
                    particles.gameObject.SetActive(true);
                    particles.ResetForPool();
                },
                actionOnRelease: particles =>
                {
                    particles.ResetForPool();
                    particles.gameObject.SetActive(false);
                },
                actionOnDestroy: particles => Destroy(particles.gameObject)
            );
        }

        public VFXStripeSkill RentStripedSkillVFX(Vector2Int source)
        {
            if (stripedSkillPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            VFXStripeSkill stripe = stripedSkillPool.Get();
            stripe.transform.position = layout.GetTileWorldPos(source.x, source.y);
            stripe.Configure(layout.TileSize);
            return stripe;
        }

        public UniTask FireStripedSkillVFX(VFXStripeSkill stripe, Vector2Int source, bool isVertical, float duration)
        {
            int negativeSideLengthInTiles = isVertical ? source.y : source.x;
            int positiveSideLengthInTiles = isVertical ? layout.Rows - source.y - 1 : layout.Cols - source.x - 1;
            int longerSideLengthInTiles = Mathf.Max(negativeSideLengthInTiles, positiveSideLengthInTiles);
            float secondsPerTile = longerSideLengthInTiles > 0 ? duration / longerSideLengthInTiles : 0f;

            Vector2Int negativeDestination = isVertical ? new Vector2Int(source.x, 0) : new Vector2Int(0, source.y);
            Vector2Int positiveDestination = isVertical ? new Vector2Int(source.x, layout.Rows - 1) : new Vector2Int(layout.Cols - 1, source.y);
            Vector2 endpointOffset = (isVertical ? Vector2.up : Vector2.right) * layout.TileSize * 0.5f;
            Vector2 negativeEndWorld = layout.GetTileWorldPos(negativeDestination.x, negativeDestination.y) - endpointOffset;
            Vector2 positiveEndWorld = layout.GetTileWorldPos(positiveDestination.x, positiveDestination.y) + endpointOffset;
            float negativeDuration = negativeSideLengthInTiles * secondsPerTile;
            float positiveDuration = positiveSideLengthInTiles * secondsPerTile;
            return stripe.Fire(negativeEndWorld, positiveEndWorld, negativeDuration, positiveDuration);
        }

        public void ReleaseStripedSkillVFX(VFXStripeSkill stripe)
        {
            stripedSkillPool.Release(stripe);
        }

        public VFXButterflySkill RentButterflySkillVFX(Transform parent)
        {
            if (butterflySkillPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            VFXButterflySkill butterfly = butterflySkillPool.Get();
            butterfly.transform.SetParent(parent, false);
            butterfly.transform.localPosition = Vector3.zero;
            butterfly.transform.localRotation = Quaternion.identity;
            butterfly.transform.localScale = Vector3.one;
            return butterfly;
        }

        public async UniTask FinishButterflySkillVFX(VFXButterflySkill butterfly, float duration)
        {
            Transform root = boardVFXRoot != null ? boardVFXRoot : transform;
            butterfly.transform.SetParent(root, true);
            ParticleSystem[] particleSystems = butterfly.GetComponentsInChildren<ParticleSystem>(true);

            try
            {
                await butterfly.Finish(duration);
                await UniTask.WaitUntil(() => !HasLivingParticles(particleSystems));
            }
            finally
            {
                foreach (ParticleSystem particles in particleSystems)
                    particles.Clear(true);

                butterflySkillPool.Release(butterfly);
            }
        }

        public VFXBubble RentBubbleVFX()
        {
            if (bubblePool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            VFXBubble bubble = bubblePool.Get();
            bubble.Configure(layout.TileSize);
            return bubble;
        }

        public void ReleaseBubbleVFX(VFXBubble bubble)
        {
            bubblePool.Release(bubble);
        }

        public void PopBubbleVFX(VFXBubble bubble)
        {
            try
            {
                bubble.Pop();
            }
            catch
            {
                bubblePool.Release(bubble);
                throw;
            }

            ReleaseBubbleAfterParticles(bubble).Forget();
        }

        private async UniTask ReleaseBubbleAfterParticles(VFXBubble bubble)
        {
            try
            {
                await bubble.WaitForEnderParticles();
            }
            finally
            {
                bubblePool.Release(bubble);
            }
        }

        public void PlayBubblePopParticles(Vector2Int position, float inflatedScaleMultiplier)
        {
            PlayBubblePopParticlesAndRelease(position, inflatedScaleMultiplier).Forget();
        }

        private async UniTask PlayBubblePopParticlesAndRelease(Vector2Int position, float inflatedScaleMultiplier)
        {
            if (bubblePopParticlesPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            VFXBubblePopParticles particles = bubblePopParticlesPool.Get();
            try
            {
                particles.transform.position = layout.GetTileWorldPos(position.x, position.y);
                particles.Configure(layout.TileSize, inflatedScaleMultiplier);
                await particles.Play();
            }
            finally
            {
                bubblePopParticlesPool.Release(particles);
            }
        }

        private static bool HasLivingParticles(IReadOnlyList<ParticleSystem> particleSystems)
        {
            foreach (ParticleSystem particles in particleSystems)
            {
                if (particles.IsAlive(false)) return true;
            }

            return false;
        }

        public async UniTask PlayMutationLaserVFX(Vector2 originPosition, IReadOnlyList<Vector2Int> targetPositions, float chargeUpDuration, float duration)
        {
            if (mutationLaserPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            Vector2 origin = layout.OriginWorldPos + originPosition * layout.TileSize;
            PlayVortex(origin, duration).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(chargeUpDuration));
            await PlayLasers(origin, targetPositions, duration - chargeUpDuration);
        }

        private async UniTask PlayVortex(Vector2 origin, float duration)
        {
            ParticleSystem originParticles = mutationLaserOriginPool.Get();
            originParticles.transform.position = origin;
            originParticles.Clear(true);
            originParticles.Play(true);

            try
            {
                float tailDuration = GetLongestParticleLifetime(originParticles);
                float emissionDuration = Mathf.Max(0f, duration - tailDuration);
                await UniTask.Delay(TimeSpan.FromSeconds(emissionDuration));
                originParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                await UniTask.WaitUntil(() => !originParticles.IsAlive(true));
            }
            finally
            {
                originParticles.Clear(true);
                mutationLaserOriginPool.Release(originParticles);
            }
        }

        private static float GetLongestParticleLifetime(ParticleSystem prefab)
        {
            float longestLifetime = 0f;

            foreach (ParticleSystem particles in prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                float simulationSpeed = Mathf.Max(main.simulationSpeed, Mathf.Epsilon);
                longestLifetime = Mathf.Max(
                    longestLifetime,
                    main.startLifetime.constantMax / simulationSpeed);
            }

            return longestLifetime;
        }

        private async UniTask PlayLasers(Vector2 origin, IReadOnlyList<Vector2Int> targetPositions, float duration)
        {
            float width = layout.TileSize * mutationLaserWidthRatio;
            var tasks = new List<UniTask>(targetPositions.Count);

            foreach (Vector2Int targetPosition in targetPositions)
            {
                Vector2 target = layout.GetTileWorldPos(targetPosition.x, targetPosition.y);
                tasks.Add(PlayLaser(origin, target, width, duration));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask PlayLaser(
            Vector2 origin,
            Vector2 target,
            float width,
            float duration)
        {
            MutationLaserView laser = mutationLaserPool.Get();

            try
            {
                await laser.Play(origin, target, width, duration);
            }
            finally
            {
                mutationLaserPool.Release(laser);
            }
        }

        private void OnDestroy()
        {
            mutationLaserPool?.Clear();
            mutationLaserOriginPool?.Clear();
            stripedSkillPool?.Clear();
            butterflySkillPool?.Clear();
            bubblePool?.Clear();
            bubblePopParticlesPool?.Clear();
        }
    }
}
