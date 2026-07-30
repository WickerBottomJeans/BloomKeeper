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
            [SerializeField] private VFXStripeBeamAxis stripedBeamAxisPrefab;
            [SerializeField] private VFXStripeHalo stripedHaloPrefab;
            [SerializeField] private VFXButterflySkill butterflySkillPrefab;
            [SerializeField] private VFXBubble bubblePrefab;
            [SerializeField] private VFXBubblePopParticles bubblePopParticlesPrefab;
            [SerializeField] private VFXPrismaticBloomProjectile prismaticBloomProjectilePrefab;
            [SerializeField] private VFXPrismaticBloomFinisher prismaticBloomFinisherPrefab;
        [SerializeField] private Transform boardVFXRoot;
            [SerializeField] private float mutationLaserWidthRatio = 0.12f;

            private ObjectPool<MutationLaserView> mutationLaserPool;
            private ObjectPool<VFXStripeBeamAxis> stripedBeamAxisPool;
            private ObjectPool<VFXStripeHalo> stripedHaloPool;
        private ObjectPool<VFXButterflySkill> butterflySkillPool;
        private ObjectPool<VFXBubble> bubblePool;
            private ObjectPool<VFXBubblePopParticles> bubblePopParticlesPool;
            private ObjectPool<VFXPrismaticBloomProjectile> prismaticBloomProjectilePool;
            private ObjectPool<VFXPrismaticBloomFinisher> prismaticBloomFinisherPool;
        private BoardLayout layout;

        public void Init(BoardLayout boardLayout)
        {
            if (mutationLaserPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a MutationLaserView prefab.");
            if (stripedBeamAxisPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a striped beam-axis prefab.");
            if (stripedHaloPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a striped halo prefab.");
            if (bubblePrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a Bubble projectile prefab.");
            if (bubblePopParticlesPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a Bubble pop-particles prefab.");
            if (prismaticBloomProjectilePrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a Prismatic Bloom projectile prefab.");
            if (prismaticBloomFinisherPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a Prismatic Bloom finisher prefab.");

            layout = boardLayout ?? throw new ArgumentNullException(nameof(boardLayout));
            Transform root = boardVFXRoot != null ? boardVFXRoot : transform;

            mutationLaserPool = new ObjectPool<MutationLaserView>(
                createFunc: () => Instantiate(mutationLaserPrefab, root),
                actionOnGet: laser => laser.gameObject.SetActive(true),
                actionOnRelease: laser => laser.gameObject.SetActive(false),
                actionOnDestroy: laser => Destroy(laser.gameObject)
            );

            stripedBeamAxisPool = new ObjectPool<VFXStripeBeamAxis>(
                createFunc: () => Instantiate(stripedBeamAxisPrefab, root),
                actionOnGet: beamAxis =>
                {
                    beamAxis.gameObject.SetActive(true);
                    beamAxis.ResetForPool();
                },
                actionOnRelease: beamAxis =>
                {
                    beamAxis.ResetForPool();
                    beamAxis.gameObject.SetActive(false);
                },
                actionOnDestroy: beamAxis => Destroy(beamAxis.gameObject)
            );

            stripedHaloPool = new ObjectPool<VFXStripeHalo>(
                createFunc: () => Instantiate(stripedHaloPrefab, root),
                actionOnGet: halo =>
                {
                    halo.gameObject.SetActive(true);
                    halo.ResetForPool();
                },
                actionOnRelease: halo =>
                {
                    halo.ResetForPool();
                    halo.gameObject.SetActive(false);
                },
                actionOnDestroy: halo => Destroy(halo.gameObject)
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

            prismaticBloomProjectilePool = new ObjectPool<VFXPrismaticBloomProjectile>(
                createFunc: () => Instantiate(prismaticBloomProjectilePrefab, root),
                actionOnGet: projectile =>
                {
                    projectile.gameObject.SetActive(true);
                    projectile.ResetForPool();
                },
                actionOnRelease: projectile =>
                {
                    projectile.ResetForPool();
                    projectile.gameObject.SetActive(false);
                },
                actionOnDestroy: projectile => Destroy(projectile.gameObject)
            );

            prismaticBloomFinisherPool = new ObjectPool<VFXPrismaticBloomFinisher>(
                createFunc: () => Instantiate(prismaticBloomFinisherPrefab, root),
                actionOnGet: finisher =>
                {
                    finisher.gameObject.SetActive(true);
                    finisher.ResetForPool();
                },
                actionOnRelease: finisher =>
                {
                    finisher.ResetForPool();
                    finisher.gameObject.SetActive(false);
                },
                actionOnDestroy: finisher => Destroy(finisher.gameObject)
            );
        }

        public VFXStripeBeamAxis RentStripedBeamAxisVFX(Vector2Int source)
        {
            if (stripedBeamAxisPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            VFXStripeBeamAxis beamAxis = stripedBeamAxisPool.Get();
            beamAxis.transform.position = layout.GetTileWorldPos(source.x, source.y);
            beamAxis.Configure(layout.TileSize);
            return beamAxis;
        }

        public VFXStripeHalo RentStripedHaloVFX(Vector2Int source)
        {
            if (stripedHaloPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            VFXStripeHalo halo = stripedHaloPool.Get();
            halo.transform.position = layout.GetTileWorldPos(source.x, source.y);
            halo.Configure(layout.TileSize);
            return halo;
        }

        public UniTask FireStripedBeamAxisVFX(VFXStripeBeamAxis beamAxis, Vector2Int source, bool isVertical, float duration)
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
            return beamAxis.Fire(negativeEndWorld, positiveEndWorld, negativeDuration, positiveDuration);
        }

        public void ReleaseStripedBeamAxisVFX(VFXStripeBeamAxis beamAxis)
        {
            stripedBeamAxisPool.Release(beamAxis);
        }

        public void ReleaseStripedHaloVFX(VFXStripeHalo halo)
        {
            stripedHaloPool.Release(halo);
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

        public async UniTask ShootPrismaticBloomProjectile(Vector3 origin, Vector3 target, float duration)
        {
            if (prismaticBloomProjectilePool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            VFXPrismaticBloomProjectile projectile = RentPrismaticBloomProjectile();
            try
            {
                projectile.Configure(layout.TileSize);
                await projectile.Shoot(origin, target, duration);
            }
            catch
            {
                ReleasePrismaticBloomProjectile(projectile);
                throw;
            }

            FinishPrismaticBloomProjectileAndRelease(projectile).Forget();
        }

        private VFXPrismaticBloomProjectile RentPrismaticBloomProjectile()
        {
            return prismaticBloomProjectilePool.Get();
        }

        private async UniTask FinishPrismaticBloomProjectileAndRelease(VFXPrismaticBloomProjectile projectile)
        {
            try
            {
                await projectile.Finish();
            }
            finally
            {
                ReleasePrismaticBloomProjectile(projectile);
            }
        }

        private void ReleasePrismaticBloomProjectile(VFXPrismaticBloomProjectile projectile)
        {
            prismaticBloomProjectilePool.Release(projectile);
        }

        public void PlayPrismaticBloomFinisher(Vector3 position)
        {
            if (prismaticBloomFinisherPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            PlayPrismaticBloomFinisherAndRelease(position).Forget();
        }

        private async UniTask PlayPrismaticBloomFinisherAndRelease(Vector3 position)
        {
            VFXPrismaticBloomFinisher finisher = prismaticBloomFinisherPool.Get();
            try
            {
                finisher.transform.position = position;
                finisher.Configure(layout.TileSize);
                await finisher.Play();
            }
            finally
            {
                prismaticBloomFinisherPool.Release(finisher);
            }
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
            await UniTask.Delay(TimeSpan.FromSeconds(chargeUpDuration));
            await PlayLasers(origin, targetPositions, duration - chargeUpDuration);
        }

        private async UniTask PlayLasers(Vector2 origin, IReadOnlyList<Vector2Int> targetPositions, float duration)
        {
            var tasks = new List<UniTask>(targetPositions.Count);

            foreach (Vector2Int targetPosition in targetPositions)
            {
                Vector2 target = layout.GetTileWorldPos(targetPosition.x, targetPosition.y);
                tasks.Add(PlayLaser(origin, target, duration));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask PlayLaser(Vector2 origin, Vector2 target, float duration)
        {
            MutationLaserView laser = mutationLaserPool.Get();

            try
            {
                laser.Configure(layout.TileSize, mutationLaserWidthRatio);
                await laser.Play(origin, target, duration);
            }
            finally
            {
                mutationLaserPool.Release(laser);
            }
        }

        private void OnDestroy()
        {
            mutationLaserPool?.Clear();
            stripedBeamAxisPool?.Clear();
            stripedHaloPool?.Clear();
            butterflySkillPool?.Clear();
            bubblePool?.Clear();
            bubblePopParticlesPool?.Clear();
            prismaticBloomProjectilePool?.Clear();
            prismaticBloomFinisherPool?.Clear();
        }
    }
}
