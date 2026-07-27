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
        private readonly BoardCell[,] grid;
        private readonly float propagationDuration;

        public StripedSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardCell[,] grid, float propagationDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.grid = grid;
            this.propagationDuration = propagationDuration;
        }

        protected override async UniTask Play(StripedRepresentationData representation, MatchGroupResolveResult resolution)
        {
            bool isVertical = representation.Direction == SpecialSkillType.StripedVertical;
            HashSet<Vector2Int> removedPositions = SkillPresentationImpactQueries.GetCurrentSkillConsumedPositions(resolution);
            HashSet<Vector2Int> changedPositions = SkillPresentationImpactQueries.GetChangedPositions(resolution);
            var patternPositions = new HashSet<Vector2Int> { representation.Source };
            removedPositions.Add(representation.Source);

            foreach (var impact in resolution.Impacts)
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
                var tileWave = new List<Vector2Int>();
                var triggeredSkillWave = new List<Vector2Int>();
                int directionCount = distance == 0 ? 1 : 2;

                for (int direction = 0; direction < directionCount; direction++)
                {
                    int offset = direction == 0 ? distance : -distance;
                    Vector2Int position = isVertical ? new Vector2Int(representation.Source.x, representation.Source.y + offset) : new Vector2Int(representation.Source.x + offset, representation.Source.y);
                    if (removedPositions.Contains(position)) petalWave.Add(position);
                    if (changedPositions.Contains(position)) tileWave.Add(position);
                    if (resolution.IsTriggeredSkillPosition(position)) triggeredSkillWave.Add(position);
                }

                tasks.Add(petalViewManager.PlayDisappearAndRelease(petalWave, stepDuration));
                tasks.Add(petalViewManager.PlayAboutToExecuteShake(triggeredSkillWave));
                tasks.Add(tileViewManager.PlayTileChanges(tileWave, grid));

                if (distance < maxDistance)
                    await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            await UniTask.WhenAll(tasks);
        }
    }
}
