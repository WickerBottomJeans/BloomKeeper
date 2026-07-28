using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public sealed class BouquetSkillPresenter : SkillRepresentationPresenter<BouquetRepresentationData>
    {
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly float disappearDuration;

        public BouquetSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, float disappearDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.disappearDuration = disappearDuration;
        }

        protected override void AcquireViews(BouquetRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            HashSet<Vector2Int> positions = SkillPresentationImpactQueries.GetCurrentSkillConsumedPositions(resolution);
            positions.Add(representation.Center);
            positions.UnionWith(resolution.GetSkillTriggerPositions());
            foreach (Vector2Int position in positions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (petalViewManager.TryAcquireView(position, nameof(BouquetSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
            }
        }

        protected override async UniTask Play(BouquetRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            HashSet<Vector2Int> removedPositions = SkillPresentationImpactQueries.GetCurrentSkillConsumedPositions(resolution);
            removedPositions.Add(representation.Center);
            removedPositions.RemoveWhere(position => !accessKeys.ContainsKey(position));
            var triggeredSkillPositions = new List<Vector2Int>();
            foreach (Vector2Int position in resolution.GetSkillTriggerPositions())
            {
                if (accessKeys.ContainsKey(position))
                    triggeredSkillPositions.Add(position);
            }
            var changes = new List<TileChange>();
            foreach (TileChange change in resolution.TileChanges)
            {
                if (change.ObstacleLayerChanged)
                    changes.Add(change);
            }
            UniTask disappearTask = petalViewManager.PlayDisappearAndRelease(new List<Vector2Int>(removedPositions), disappearDuration, accessKeys);
            UniTask triggeredSkillTask = petalViewManager.PlayAboutToExecuteShake(triggeredSkillPositions, accessKeys);
            UniTask bloomTask = boardVFXManager.PlayBouquetBloomVFX(representation.Center);
            UniTask tileTask = tileViewManager.PlayTileChanges(changes);
            await UniTask.WhenAll(disappearTask, triggeredSkillTask, bloomTask, tileTask);
        }
    }
}
