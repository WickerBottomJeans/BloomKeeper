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
    public class PrismaticBloomSkillPresenter : SkillAnimation<PrismaticBloomRepresentationData>
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

        protected override IEnumerable<Vector2Int> AdditionalViews(PrismaticBloomRepresentationData representation, BoardChangeGroup effect)
        {
            return GetTargets(representation, effect);
        }

        protected override async UniTask Play(PrismaticBloomRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            List<Vector2Int> targets = GetTargets(representation, skillPresentationContext.BoardChangeGroup);
            await Prepare(representation.Source, skillPresentationContext.Views, skillPresentationContext.AudioPlaybackScope);
            await Fire(representation, targets, skillPresentationContext);
        }

        private UniTask Prepare(Vector2Int source, PetalViewScope views, AudioPlaybackScope audioScope)
        {
            AudioService.Instance.PlaySfx(audioCue, audioScope);
            return petalViewManager.PlayPrismaticBloomPrepareSpin(source, PrepareDuration, MaximumSpinSpeed, views);
        }

        private async UniTask Fire(PrismaticBloomRepresentationData representation, IReadOnlyList<Vector2Int> targets, SkillPresentationContext skillPresentationContext)
        {
            PetalViewScope views = skillPresentationContext.Views;
            Vector2Int source = representation.Source;
            Vector3 origin = layout.GetTileWorldPosition(source.x, source.y);
            UniTask spinTask = petalViewManager.PlayPrismaticBloomFireSpin(source, FireDuration, MaximumSpinSpeed, views);
            var projectileTasks = new List<UniTask>(targets.Count);

            foreach (Vector2Int target in targets)
            {
                Vector3 targetWorldPosition = layout.GetTileWorldPosition(target.x, target.y);
                projectileTasks.Add(PlayProjectile(origin, targetWorldPosition, target, FireDuration, representation, skillPresentationContext));
            }

            await spinTask;
            AudioService.Instance.PlaySfx(finishCue, skillPresentationContext.AudioPlaybackScope);
            boardVFXManager.PlayPrismaticBloomFinisher(origin);
            UniTask sourceDisappearTask = petalViewManager.PlayNormalRemovals(new[] { source }, views);
            skillPresentationContext.HandleConsumedInputPresented(source);
            projectileTasks.Add(sourceDisappearTask);
            await UniTask.WhenAll(projectileTasks);
        }

        private async UniTask PlayProjectile(Vector3 origin, Vector3 targetWorldPosition, Vector2Int target, float duration, PrismaticBloomRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            await boardVFXManager.ShootPrismaticBloomProjectile(origin, targetWorldPosition, duration);
            await PlayTargetImpact(target, representation, skillPresentationContext);
        }

        private async UniTask PlayTargetImpact(Vector2Int target, PrismaticBloomRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            BoardChangeGroup resolution = skillPresentationContext.BoardChangeGroup;
            PetalViewScope views = skillPresentationContext.Views;
            if (representation.ReplacementSkill != SpecialSkillType.None)
            {
                PetalChange change = GetChangeAt(representation, target);
                await petalViewManager.PlayPetalChange(change, layout, views);
                await petalViewManager.PlayAboutToExecute(new[] { target }, views);
                skillPresentationContext.HandleTileImpactPresented(target);
                return;
            }

            UniTask impactTask = resolution.IsTriggeredSkillInputPosition(target) ? petalViewManager.PlayAboutToExecute(new[] { target }, views) : petalViewManager.PlayNormalRemovals(new[] { target }, views);
            skillPresentationContext.HandleTileImpactPresented(target);
            await impactTask;
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

        private  List<Vector2Int> GetTargets(PrismaticBloomRepresentationData representation, BoardChangeGroup resolution)
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
