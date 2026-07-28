using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public sealed class StripedSkillPresenter : SkillRepresentationPresenter<StripedRepresentationData>
    {
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly float propagationDuration;

        public StripedSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, float propagationDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.propagationDuration = propagationDuration;
        }

        protected override void AcquireViews(StripedRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            HashSet<Vector2Int> positions = SkillPresentationImpactQueries.GetCurrentSkillConsumedPositions(resolution);
            positions.Add(representation.Source);
            positions.UnionWith(resolution.GetSkillTriggerPositions());
            foreach (Vector2Int position in positions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (petalViewManager.TryAcquireView(position, nameof(StripedSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
            }
        }

        protected override async UniTask Play(StripedRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            bool isVertical = representation.Direction == SpecialSkillType.StripedVertical;
            HashSet<Vector2Int> removedPositions = SkillPresentationImpactQueries.GetCurrentSkillConsumedPositions(resolution);
            IReadOnlyList<TileChange> tileChanges = resolution.TileChanges;
            var patternPositions = new HashSet<Vector2Int> { representation.Source };
            removedPositions.Add(representation.Source);
            removedPositions.RemoveWhere(position => !accessKeys.ContainsKey(position));

            foreach (var impact in resolution.TileChanges)
                patternPositions.Add(impact.Position);

            int maxDistance = 0;
            foreach (Vector2Int position in patternPositions)
            {
                int distance = isVertical ? Mathf.Abs(position.y - representation.Source.y) : Mathf.Abs(position.x - representation.Source.x);
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            float stepDuration = maxDistance > 0 ? propagationDuration / maxDistance : propagationDuration;
            var tasks = new List<UniTask> { boardVFXManager.PlayStripedSkillVFX(representation.Source, isVertical, propagationDuration) };

            for (int distance = 0; distance <= maxDistance; distance++)
            {
                var petalWave = new List<Vector2Int>();
                var tileWave = new List<TileChange>();
                var triggeredSkillWave = new List<Vector2Int>();
                int directionCount = distance == 0 ? 1 : 2;

                for (int direction = 0; direction < directionCount; direction++)
                {
                    int offset = direction == 0 ? distance : -distance;
                    Vector2Int position = isVertical ? new Vector2Int(representation.Source.x, representation.Source.y + offset) : new Vector2Int(representation.Source.x + offset, representation.Source.y);
                    if (removedPositions.Contains(position)) petalWave.Add(position);
                    foreach (TileChange change in tileChanges)
                    {
                        if (change.Position == position && change.ObstacleLayerChanged)
                            tileWave.Add(change);
                    }
                    if (resolution.IsTriggeredSkillPosition(position) && accessKeys.ContainsKey(position)) triggeredSkillWave.Add(position);
                }

                tasks.Add(petalViewManager.PlayDisappearAndRelease(petalWave, stepDuration, accessKeys));
                tasks.Add(petalViewManager.PlayAboutToExecuteShake(triggeredSkillWave, accessKeys));
                tasks.Add(tileViewManager.PlayTileChanges(tileWave));

                if (distance < maxDistance)
                    await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            await UniTask.WhenAll(tasks);
        }
    }
}
