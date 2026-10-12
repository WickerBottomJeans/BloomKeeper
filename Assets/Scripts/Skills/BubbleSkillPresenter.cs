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
    public class BubbleSkillPresenter : SkillAnimation<BubbleRepresentationData>
    {
        private const float BubbleInflateScaleMultiplier = 1.5f;
        private const float PrepareDuration = 0.4f;
        private const float FireDuration = 0.3f;
        private const float FinishDuration = 0.2f;

        private readonly PieceViewManager pieceViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout layout;

        public BubbleSkillPresenter(PieceViewManager pieceViewManager, BoardVFXManager boardVFXManager, BoardAudioManager boardAudioManager, BoardLayout layout)
        {
            this.pieceViewManager = pieceViewManager;
            this.boardVFXManager = boardVFXManager;
            this.boardAudioManager = boardAudioManager;
            this.layout = layout;
        }

        protected override async UniTask Play(BubbleRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            var bubbles = new Dictionary<Vector2Int, VFXBubble>();

            try
            {
                await Prepare(representation.Center, skillPresentationContext.Views, skillPresentationContext.AudioPlaybackScope);
                await Fire(representation.Center, bubbles, skillPresentationContext);
                await Finish(representation.Center, bubbles, skillPresentationContext);
            }
            finally
            {
                foreach (VFXBubble bubble in bubbles.Values)
                    boardVFXManager.ReleaseBubbleVFX(bubble);
            }
        }

        private UniTask Prepare(Vector2Int center, PieceViewScope views, AudioPlaybackScope audioScope)
        {
            boardAudioManager.PlayBubblePrepare(audioScope);
            return views.Owns(center) ? PetalPresentation.PlayBubbleInflate(pieceViewManager, center, BubbleInflateScaleMultiplier, PrepareDuration, views) : UniTask.CompletedTask;
        }

        private async UniTask Fire(Vector2Int center, IDictionary<Vector2Int, VFXBubble> bubbles, SkillPresentationContext skillPresentationContext)
        {
            BoardChangeGroup resolution = skillPresentationContext.BoardChangeGroup;
            PieceViewScope views = skillPresentationContext.Views;
            if (views.Owns(center))
            {
                PetalPresentation.HideBubbleForPop(pieceViewManager, center, views);
                boardVFXManager.PlayBubblePopParticles(center, BubbleInflateScaleMultiplier, skillPresentationContext.AudioPlaybackScope);
            }
            skillPresentationContext.HandleTileImpactPresented(center);

            Vector3 origin = layout.GetTileWorldPosition(center.x, center.y);
            HashSet<Vector2Int> targetPositions = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(resolution);
            targetPositions.Remove(center);
            foreach (Vector2Int position in resolution.GetTriggeredSkillInputPositions())
            {
                if (position != center)
                    targetPositions.Add(position);
            }

            var tasks = new List<UniTask>(targetPositions.Count);

            foreach (Vector2Int position in targetPositions)
            {
                VFXBubble bubble = boardVFXManager.RentBubbleVFX();
                bubbles.Add(position, bubble);
                Vector3 target = layout.GetTileWorldPosition(position.x, position.y);
                tasks.Add(bubble.Shoot(origin, target, FireDuration));
            }

            try
            {
                await UniTask.WhenAll(tasks);
            }
            finally
            {
                if (views.Owns(center))
                    pieceViewManager.ReleasePieceViewsImmediately(new[] { center }, views);
            }
        }

        private async UniTask Finish(Vector2Int center, IDictionary<Vector2Int, VFXBubble> bubbles, SkillPresentationContext skillPresentationContext)
        {
            BoardChangeGroup resolution = skillPresentationContext.BoardChangeGroup;
            PieceViewScope views = skillPresentationContext.Views;
            HashSet<Vector2Int> removedPositions = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(resolution);
            removedPositions.Remove(center);
            removedPositions = new HashSet<Vector2Int>(views.AcquireAvailable(removedPositions));
            List<Vector2Int> triggeredSkillPositions = views.AcquireAvailable(resolution.TriggeredSkillPositions);
            triggeredSkillPositions.Remove(center);
            foreach (TileChange change in resolution.TileChanges)
            {
                if (change.FeatureChanged)
                    skillPresentationContext.HandleTileImpactPresented(change.Position);
            }

            var tasks = new List<UniTask>(2);
            tasks.Add(PopBubblesOverDuration(bubbles, removedPositions, skillPresentationContext));
            tasks.Add(PetalPresentation.PlayAboutToExecute(pieceViewManager, triggeredSkillPositions, views));
            await UniTask.WhenAll(tasks);
        }

        private async UniTask PopBubblesOverDuration(IDictionary<Vector2Int, VFXBubble> bubbles, ISet<Vector2Int> removedPositions, SkillPresentationContext skillPresentationContext)
        {
            var popDelays = new Dictionary<Vector2Int, float>(bubbles.Count);
            var popOrder = new List<Vector2Int>(bubbles.Keys);
            foreach (Vector2Int position in popOrder)
                popDelays.Add(position, UnityEngine.Random.Range(0f, FinishDuration));
            popOrder.Sort((left, right) => popDelays[left].CompareTo(popDelays[right]));

            float elapsed = 0f;
            foreach (Vector2Int position in popOrder)
            {
                float delay = popDelays[position] - elapsed;
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
                elapsed = popDelays[position];

                VFXBubble bubble = bubbles[position];
                bubbles.Remove(position);
                boardVFXManager.PopBubbleVFX(bubble, skillPresentationContext.AudioPlaybackScope);
                if (removedPositions.Contains(position))
                    pieceViewManager.ReleasePieceViewsImmediately(new[] { position }, skillPresentationContext.Views);
                skillPresentationContext.HandleTileImpactPresented(position);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(FinishDuration - elapsed));
        }
    }
}
