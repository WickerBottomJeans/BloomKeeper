using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public sealed class PrismaticBloomSkillPresenter : SkillRepresentationPresenter<PrismaticBloomRepresentationData>
    {
        private const float LaunchWindowRatio = 0.3f;
        private const float ProjectileTravelDurationRatio = 0.7f;

        private readonly PetalViewManager petalViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardLayout layout;
        private readonly float prepareDuration;
        private readonly float fireDuration;
        private readonly float maximumSpinSpeed;

        public PrismaticBloomSkillPresenter(PetalViewManager petalViewManager, BoardVFXManager boardVFXManager, BoardLayout layout, float prepareDuration, float fireDuration, float maximumSpinSpeed)
        {
            this.petalViewManager = petalViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
            this.prepareDuration = prepareDuration;
            this.fireDuration = fireDuration;
            this.maximumSpinSpeed = maximumSpinSpeed;
        }

        protected override void AcquireAdditionalVitalViews(PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var requiredPositions = new HashSet<Vector2Int>(GetTargets(representation, resolution));
            foreach (Vector2Int position in requiredPositions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (!petalViewManager.TryAcquireView(position, nameof(PrismaticBloomSkillPresenter), out ViewAccessKey accessKey))
                    throw new InvalidOperationException($"Petal view at {position} cannot be acquired by {nameof(PrismaticBloomSkillPresenter)}.");
                accessKeys.Add(position, accessKey);
            }
        }

        protected override async UniTask Play(PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            List<Vector2Int> targets = GetTargets(representation, resolution);
            Shuffle(targets);
            await Prepare(representation.Source, accessKeys);
            await Fire(representation, resolution, targets, accessKeys);
        }

        private UniTask Prepare(Vector2Int source, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            return petalViewManager.PlayPrismaticBloomPrepareSpin(source, prepareDuration, maximumSpinSpeed, accessKeys);
        }

        private async UniTask Fire(PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution, IReadOnlyList<Vector2Int> targets, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            Vector2Int source = representation.Source;
            Vector3 origin = layout.GetTileWorldPos(source.x, source.y);
            float projectileTravelDuration = fireDuration * ProjectileTravelDurationRatio;
            float launchWindowDuration = fireDuration * LaunchWindowRatio;
            float launchInterval = targets.Count > 1 ? launchWindowDuration / (targets.Count - 1) : 0f;
            UniTask fireTimer = UniTask.Delay(TimeSpan.FromSeconds(fireDuration));
            UniTask spinTask = petalViewManager.PlayPrismaticBloomFireSpin(source, fireDuration, maximumSpinSpeed, accessKeys);
            var projectileTasks = new List<UniTask>(targets.Count);

            for (int i = 0; i < targets.Count; i++)
            {
                Vector2Int target = targets[i];
                Vector3 targetWorldPosition = layout.GetTileWorldPos(target.x, target.y);
                projectileTasks.Add(PlayProjectile(origin, targetWorldPosition, target, projectileTravelDuration, representation, resolution, accessKeys));
                if (i < targets.Count - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(launchInterval));
            }

            await UniTask.WhenAll(fireTimer, spinTask);
            boardVFXManager.PlayPrismaticBloomFinisher(origin);
            UniTask sourceDisappearTask = petalViewManager.PlayNormalRemovals(new[] { source }, accessKeys);
            projectileTasks.Add(sourceDisappearTask);
            await UniTask.WhenAll(projectileTasks);
        }

        private async UniTask PlayProjectile(Vector3 origin, Vector3 targetWorldPosition, Vector2Int target, float duration, PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            await boardVFXManager.ShootPrismaticBloomProjectile(origin, targetWorldPosition, duration);
            await PlayTargetImpact(target, representation, resolution, accessKeys);
        }

        private async UniTask PlayTargetImpact(Vector2Int target, PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            if (representation.ReplacementSkill != SpecialSkillType.None)
            {
                PetalChange change = GetChangeAt(representation, target);
                await petalViewManager.PlayPetalChange(change, layout, accessKeys);
                await petalViewManager.PlayAboutToExecute(new[] { target }, accessKeys);
                return;
            }

            if (resolution.IsTriggeredSkillInputPosition(target))
                await petalViewManager.PlayAboutToExecute(new[] { target }, accessKeys);
            else
                await petalViewManager.PlayNormalRemovals(new[] { target }, accessKeys);
        }

        private static PetalChange GetChangeAt(PrismaticBloomRepresentationData representation, Vector2Int target)
        {
            foreach (PetalChange change in representation.Changes)
            {
                if (change.Position == target)
                    return change;
            }

            throw new InvalidOperationException($"Prismatic Bloom has no petal change for target {target}.");
        }

        private static List<Vector2Int> GetTargets(PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution)
        {
            List<Vector2Int> targets;
            if (representation.ReplacementSkill == SpecialSkillType.None)
            {
                targets = new List<Vector2Int>(SkillPresentationQueries.GetRemovedPositions(resolution));
            }
            else
            {
                targets = new List<Vector2Int>(representation.Changes.Count);
                foreach (PetalChange change in representation.Changes)
                    targets.Add(change.Position);
            }

            targets.Remove(representation.Source);
            return targets;
        }

        private static void Shuffle(IList<Vector2Int> positions)
        {
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (positions[i], positions[randomIndex]) = (positions[randomIndex], positions[i]);
            }
        }
    }
}
