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
    public sealed class BubbleSkillPresenter : SkillRepresentationPresenter<BubbleRepresentationData>
    {
        private const float BubbleInflateScaleMultiplier = 1.5f;

        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout layout;
        private readonly float prepareDuration;
        private readonly float fireDuration;
        private readonly float finishDuration;

        public BubbleSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardAudioManager boardAudioManager, BoardLayout layout, float prepareDuration, float fireDuration, float finishDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.boardAudioManager = boardAudioManager;
            this.layout = layout;
            this.prepareDuration = prepareDuration;
            this.fireDuration = fireDuration;
            this.finishDuration = finishDuration;
        }

        protected override async UniTask Play(BubbleRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            var bubbles = new Dictionary<Vector2Int, VFXBubble>();

            try
            {
                await Prepare(representation.Center, accessKeys, audioScope);
                await Fire(representation.Center, resolution, bubbles, accessKeys, audioScope);
                await Finish(representation.Center, resolution, bubbles, accessKeys, audioScope);
            }
            finally
            {
                foreach (VFXBubble bubble in bubbles.Values)
                    boardVFXManager.ReleaseBubbleVFX(bubble);
            }
        }

        private UniTask Prepare(Vector2Int center, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            boardAudioManager.PlayBubblePrepare(audioScope);
            return accessKeys.ContainsKey(center) ? petalViewManager.PlayBubbleInflate(center, BubbleInflateScaleMultiplier, prepareDuration, accessKeys) : UniTask.CompletedTask;
        }

        private async UniTask Fire(Vector2Int center, MatchGroupResolveResult resolution, IDictionary<Vector2Int, VFXBubble> bubbles, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            if (accessKeys.ContainsKey(center))
            {
                petalViewManager.HideBubbleForPop(center, accessKeys);
                boardVFXManager.PlayBubblePopParticles(center, BubbleInflateScaleMultiplier, audioScope);
            }

            Vector3 origin = layout.GetTileWorldPos(center.x, center.y);
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
                Vector3 target = layout.GetTileWorldPos(position.x, position.y);
                tasks.Add(bubble.Shoot(origin, target, fireDuration));
            }

            try
            {
                await UniTask.WhenAll(tasks);
            }
            finally
            {
                if (accessKeys.ContainsKey(center))
                    petalViewManager.ReleasePetalViewsImmediately(new[] { center }, accessKeys);
            }
        }

        private async UniTask Finish(Vector2Int center, MatchGroupResolveResult resolution, IDictionary<Vector2Int, VFXBubble> bubbles, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            HashSet<Vector2Int> removedPositions = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(resolution);
            removedPositions.Remove(center);
            foreach (Vector2Int position in removedPositions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (petalViewManager.TryAcquireView(position, nameof(BubbleSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
            }
            removedPositions.RemoveWhere(position => !accessKeys.ContainsKey(position));
            var triggeredSkillPositions = new List<Vector2Int>();
            foreach (Vector2Int position in resolution.GetTriggeredSkillInputPositions())
            {
                if (!accessKeys.ContainsKey(position) && petalViewManager.TryAcquireView(position, nameof(BubbleSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
                if (position != center && accessKeys.ContainsKey(position))
                    triggeredSkillPositions.Add(position);
            }
            var changes = new List<TileChange>();
            foreach (TileChange change in resolution.TileChanges)
            {
                if (change.ObstacleLayerChanged)
                    changes.Add(change);
            }

            var tasks = new List<UniTask>(3);
            tasks.Add(PopBubblesOverDuration(bubbles, removedPositions, accessKeys, audioScope));
            tasks.Add(petalViewManager.PlayAboutToExecute(triggeredSkillPositions, accessKeys));
            tasks.Add(tileViewManager.PlayTileChanges(changes));
            await UniTask.WhenAll(tasks);
        }

        private async UniTask PopBubblesOverDuration(IDictionary<Vector2Int, VFXBubble> bubbles, ISet<Vector2Int> removedPositions, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            var popDelays = new Dictionary<Vector2Int, float>(bubbles.Count);
            var popOrder = new List<Vector2Int>(bubbles.Keys);
            foreach (Vector2Int position in popOrder)
                popDelays.Add(position, UnityEngine.Random.Range(0f, finishDuration));
            popOrder.Sort((left, right) => popDelays[left].CompareTo(popDelays[right]));

            float elapsed = 0f;
            foreach (Vector2Int position in popOrder)
            {
                float delay = popDelays[position] - elapsed;
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
                elapsed = popDelays[position];

                VFXBubble bubble = bubbles[position];
                bubbles.Remove(position);
                boardVFXManager.PopBubbleVFX(bubble, audioScope);
                if (removedPositions.Contains(position))
                    petalViewManager.ReleasePetalViewsImmediately(new[] { position }, accessKeys);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(finishDuration - elapsed));
        }
    }
}
