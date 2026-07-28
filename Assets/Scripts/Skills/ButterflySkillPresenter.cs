using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public sealed class ButterflySkillPresenter : SkillRepresentationPresenter<ButterflyRepresentationData>
    {
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardLayout layout;
        private readonly float flightDuration;
        private readonly float disappearDuration;

        public ButterflySkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardLayout layout, float flightDuration, float disappearDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.layout = layout;
            this.flightDuration = flightDuration;
            this.disappearDuration = disappearDuration;
        }

        protected override void AcquireVitalViews(ButterflyRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            if (accessKeys.ContainsKey(representation.Source)) return;
            if (petalViewManager.TryAcquireView(representation.Source, nameof(ButterflySkillPresenter), out ViewAccessKey accessKey))
                accessKeys.Add(representation.Source, accessKey);
        }

        protected override async UniTask Play(ButterflyRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var changes = new List<TileChange>();
            foreach (TileChange change in resolution.TileChanges)
            {
                if (change.ObstacleLayerChanged)
                    changes.Add(change);
            }
            if (!representation.Target.HasValue)
            {
                IReadOnlyList<Vector2Int> source = accessKeys.ContainsKey(representation.Source) ? new[] { representation.Source } : System.Array.Empty<Vector2Int>();
                await UniTask.WhenAll(petalViewManager.PlayDisappearAndRelease(source, disappearDuration, accessKeys), tileViewManager.PlayTileChanges(changes));
                return;
            }

            if (accessKeys.ContainsKey(representation.Source))
                await petalViewManager.PlayFly(representation.Source, representation.Target.Value, layout, flightDuration, accessKeys);
            Vector2Int target = representation.Target.Value;
            bool targetNeedsView = target != representation.Source && (SkillPresentationQueries.WasPetalRemovedAt(resolution, target) || resolution.IsTriggeredSkillPosition(target));
            if (targetNeedsView && !accessKeys.ContainsKey(target) && petalViewManager.TryAcquireView(target, nameof(ButterflySkillPresenter), out ViewAccessKey targetAccessKey))
                accessKeys.Add(target, targetAccessKey);
            var disappearingPositions = new List<Vector2Int>();
            if (accessKeys.ContainsKey(representation.Source))
                disappearingPositions.Add(representation.Source);
            var triggeredSkillPositions = new List<Vector2Int>();
            if (SkillPresentationQueries.WasPetalRemovedAt(resolution, target) && target != representation.Source && !resolution.IsTriggeredSkillPosition(target) && accessKeys.ContainsKey(target))
                disappearingPositions.Add(target);
            if (resolution.IsTriggeredSkillPosition(target) && target != representation.Source && accessKeys.ContainsKey(target))
                triggeredSkillPositions.Add(target);

            await UniTask.WhenAll(petalViewManager.PlayDisappearAndRelease(disappearingPositions, disappearDuration, accessKeys), petalViewManager.PlayAboutToExecuteShake(triggeredSkillPositions, accessKeys), tileViewManager.PlayTileChanges(changes));
        }
    }
}
