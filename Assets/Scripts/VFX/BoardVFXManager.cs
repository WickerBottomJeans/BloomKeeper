using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

namespace DefaultNamespace.VFX
{
    //TODO: we need to care for the case skill A destroy petal skill B. we need to leave B alone so it can run its own VFX next cascade
    public sealed class BoardVFXManager : MonoBehaviour
    {
        [SerializeField] private MutationLaserView mutationLaserPrefab;
        [SerializeField] private ParticleSystem mutationLaserOriginPrefab;
        [SerializeField] private GameObject bouquetBloomPrefab;
        [SerializeField] private VFXStripeSkill stripedSkillPrefab;
        [SerializeField] private Transform laserRoot;
        [SerializeField] private float mutationLaserWidthRatio = 0.12f;

        private ObjectPool<MutationLaserView> mutationLaserPool;
        private ObjectPool<ParticleSystem> mutationLaserOriginPool;
        private ObjectPool<GameObject> bouquetBloomPool;
        private ObjectPool<VFXStripeSkill> stripedSkillPool;
        private BoardLayout layout;

        public void Init(BoardLayout boardLayout)
        {
            if (mutationLaserPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a MutationLaserView prefab.");
            if (mutationLaserOriginPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a mutation laser origin ParticleSystem prefab.");
            if (bouquetBloomPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a Bomb bloom prefab.");
            if (stripedSkillPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a striped skill prefab.");

            layout = boardLayout ?? throw new ArgumentNullException(nameof(boardLayout));
            Transform root = laserRoot != null ? laserRoot : transform;

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

            bouquetBloomPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(bouquetBloomPrefab, root),
                actionOnGet: bloom => bloom.SetActive(true),
                actionOnRelease: bloom => bloom.SetActive(false),
                actionOnDestroy: Destroy
            );

            stripedSkillPool = new ObjectPool<VFXStripeSkill>(
                createFunc: () => Instantiate(stripedSkillPrefab, root),
                actionOnGet: stripe => stripe.gameObject.SetActive(true),
                actionOnRelease: stripe => stripe.gameObject.SetActive(false),
                actionOnDestroy: stripe => Destroy(stripe.gameObject)
            );
        }

        public async UniTask PlayStripedSkillVFX(Vector2Int source, bool isVertical, float duration)
        {
            if (stripedSkillPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            int negativeSideLengthInCells = isVertical ? source.y : source.x;
            int positiveSideLengthInCells = isVertical ? layout.Rows - source.y - 1 : layout.Cols - source.x - 1;
            int longerSideLengthInCells = Mathf.Max(negativeSideLengthInCells, positiveSideLengthInCells);
            float secondsPerCell = longerSideLengthInCells > 0 ? duration / longerSideLengthInCells : 0f;

            Vector2Int negativeDestination = isVertical ? new Vector2Int(source.x, 0) : new Vector2Int(0, source.y);
            Vector2Int positiveDestination = isVertical ? new Vector2Int(source.x, layout.Rows - 1) : new Vector2Int(layout.Cols - 1, source.y);
            Vector2 negativeDirection = isVertical ? Vector2.down : Vector2.left;
            Vector2 positiveDirection = isVertical ? Vector2.up : Vector2.right;

            await UniTask.WhenAll(
                PlayStripedSide(source, negativeDestination, negativeDirection, negativeSideLengthInCells * secondsPerCell),
                PlayStripedSide(source, positiveDestination, positiveDirection, positiveSideLengthInCells * secondsPerCell));
        }

        private async UniTask PlayStripedSide(Vector2Int source, Vector2Int destination, Vector2 travelDirection, float duration)
        {
            VFXStripeSkill stripe = stripedSkillPool.Get();
            stripe.transform.position = layout.GetCellWorldPos(source.x, source.y);
            stripe.Prepare(travelDirection, layout.CellSize);

            try
            {
                Vector2 destinationWorldPosition = layout.GetCellWorldPos(destination.x, destination.y);
                await stripe.transform.DOMove(destinationWorldPosition, duration).SetEase(Ease.Linear).ToUniTask();
            }
            finally
            {
                stripedSkillPool.Release(stripe);
            }
        }

        public async UniTask PlayBouquetBloomVFX(Vector2Int center)
        {
            if (bouquetBloomPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            GameObject bloom = bouquetBloomPool.Get();
            ParticleSystem[] particleSystems = bloom.GetComponentsInChildren<ParticleSystem>(true);

            try
            {
                bloom.transform.position = layout.GetCellWorldPos(center.x, center.y);

                foreach (ParticleSystem particles in particleSystems)
                {
                    particles.Clear(false);
                    particles.Play(false);
                }

                await UniTask.WaitUntil(() => !HasLivingParticles(particleSystems));
            }
            finally
            {
                //TODO: test this again later, null error or sth idk
                foreach (ParticleSystem particles in particleSystems)
                    particles.Clear(false);

                bouquetBloomPool.Release(bloom);
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

            Vector2 origin = layout.OriginWorldPos + originPosition * layout.CellSize;
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
            float width = layout.CellSize * mutationLaserWidthRatio;
            var tasks = new List<UniTask>(targetPositions.Count);

            foreach (Vector2Int targetPosition in targetPositions)
            {
                Vector2 target = layout.GetCellWorldPos(targetPosition.x, targetPosition.y);
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
            bouquetBloomPool?.Clear();
            stripedSkillPool?.Clear();
        }
    }
}
