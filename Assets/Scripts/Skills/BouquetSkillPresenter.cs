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

        protected override void AcquireVitalViews(BouquetRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            if (accessKeys.ContainsKey(representation.Center)) return;
            if (petalViewManager.TryAcquireView(representation.Center, nameof(BouquetSkillPresenter), out ViewAccessKey accessKey))
                accessKeys.Add(representation.Center, accessKey);
        }

        protected override async UniTask Play(BouquetRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            HashSet<Vector2Int> removedPositions = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(resolution);
            removedPositions.Add(representation.Center);
            foreach (Vector2Int position in removedPositions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (petalViewManager.TryAcquireView(position, nameof(BouquetSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
            }
            removedPositions.RemoveWhere(position => !accessKeys.ContainsKey(position));
            var triggeredSkillPositions = new List<Vector2Int>();
            foreach (Vector2Int position in resolution.GetSkillTriggerPositions())
            {
                if (!accessKeys.ContainsKey(position) && petalViewManager.TryAcquireView(position, nameof(BouquetSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
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
