using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.Pool;

namespace DefaultNamespace.VFX
{
    public sealed class BoardVFXManager : MonoBehaviour
    {
        [SerializeField] private MutationLaserView mutationLaserPrefab;
        [SerializeField] private ParticleSystem mutationLaserOriginPrefab;
        [SerializeField] private Transform laserRoot;
        [SerializeField] private float mutationLaserWidthRatio = 0.12f;

        private ObjectPool<MutationLaserView> mutationLaserPool;
        private ObjectPool<ParticleSystem> mutationLaserOriginPool;
        private BoardLayout layout;

        public void Init(BoardLayout boardLayout)
        {
            if (mutationLaserPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a MutationLaserView prefab.");
            if (mutationLaserOriginPrefab == null)
                throw new InvalidOperationException("BoardVFXManager requires a mutation laser origin ParticleSystem prefab.");

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
        }

        public async UniTask PlayMutationLaserVFX(
            Vector2 originPosition,
            IReadOnlyList<PetalChange> changes,
            float chargeUpDuration,
            float duration)
        {
            if (mutationLaserPool == null)
                throw new InvalidOperationException("BoardVFXManager must be initialized before playing effects.");

            Vector2 origin = layout.OriginWorldPos + originPosition * layout.CellSize;
            PlayVortex(origin, duration).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(chargeUpDuration));
            await PlayLasers(origin, changes, duration - chargeUpDuration);
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

        private async UniTask PlayLasers(
            Vector2 origin,
            IReadOnlyList<PetalChange> changes,
            float duration)
        {
            float width = layout.CellSize * mutationLaserWidthRatio;
            var tasks = new List<UniTask>(changes.Count);

            foreach (PetalChange change in changes)
            {
                Vector2Int targetPosition = change.Position;
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
        }
    }
}
