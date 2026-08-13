using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public class PrismaticBloomSkillPresenter : SkillRepresentationPresenter<PrismaticBloomRepresentationData>
    {
        private const float PrepareDuration = 0.2f;
        private const float FireDuration = 0.3f;
        private const float MaximumSpinSpeed = 1200f;

        private readonly PetalViewManager petalViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardLayout layout;
        private readonly AudioCue audioCue;
        private readonly AudioCue finishCue;

        public PrismaticBloomSkillPresenter(PetalViewManager petalViewManager, BoardVFXManager boardVFXManager, BoardLayout layout, AudioCue audioCue, AudioCue finishCue)
        {
            this.petalViewManager = petalViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
            this.audioCue = audioCue;
            this.finishCue = finishCue;
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

        protected override async UniTask Play(PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            List<Vector2Int> targets = GetTargets(representation, resolution);
            await Prepare(representation.Source, accessKeys, audioScope);
            await Fire(representation, resolution, targets, accessKeys, audioScope);
        }

        private UniTask Prepare(Vector2Int source, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            AudioService.Instance.PlaySfx(audioCue, audioScope);
            return petalViewManager.PlayPrismaticBloomPrepareSpin(source, PrepareDuration, MaximumSpinSpeed, accessKeys);
        }

        private async UniTask Fire(PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution, IReadOnlyList<Vector2Int> targets, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            Vector2Int source = representation.Source;
            Vector3 origin = layout.GetTileWorldPos(source.x, source.y);
            UniTask spinTask = petalViewManager.PlayPrismaticBloomFireSpin(source, FireDuration, MaximumSpinSpeed, accessKeys);
            var projectileTasks = new List<UniTask>(targets.Count);

            foreach (Vector2Int target in targets)
            {
                Vector3 targetWorldPosition = layout.GetTileWorldPos(target.x, target.y);
                projectileTasks.Add(PlayProjectile(origin, targetWorldPosition, target, FireDuration, representation, resolution, accessKeys));
            }

            await spinTask;
            AudioService.Instance.PlaySfx(finishCue, audioScope);
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

        private  PetalChange GetChangeAt(PrismaticBloomRepresentationData representation, Vector2Int target)
        {
            foreach (PetalChange change in representation.Changes)
            {
                if (change.Position == target)
                    return change;
            }

            throw new InvalidOperationException($"Prismatic Bloom has no petal change for target {target}.");
        }

        private  List<Vector2Int> GetTargets(PrismaticBloomRepresentationData representation, MatchGroupResolveResult resolution)
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

    }
}
