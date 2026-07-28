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

        protected override void AcquireViews(ButterflyRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var positions = new List<Vector2Int> { representation.Source };
            if (representation.Target.HasValue && (SkillPresentationImpactQueries.WasPetalRemovedAt(resolution, representation.Target.Value) || resolution.IsTriggeredSkillPosition(representation.Target.Value)))
                positions.Add(representation.Target.Value);
            foreach (Vector2Int position in positions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (petalViewManager.TryAcquireView(position, nameof(ButterflySkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
            }
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
            var disappearingPositions = new List<Vector2Int>();
            if (accessKeys.ContainsKey(representation.Source))
                disappearingPositions.Add(representation.Source);
            var triggeredSkillPositions = new List<Vector2Int>();
            if (SkillPresentationImpactQueries.WasPetalRemovedAt(resolution, representation.Target.Value) && representation.Target.Value != representation.Source && !resolution.IsTriggeredSkillPosition(representation.Target.Value) && accessKeys.ContainsKey(representation.Target.Value))
                disappearingPositions.Add(representation.Target.Value);
            if (resolution.IsTriggeredSkillPosition(representation.Target.Value) && representation.Target.Value != representation.Source && accessKeys.ContainsKey(representation.Target.Value))
                triggeredSkillPositions.Add(representation.Target.Value);

            await UniTask.WhenAll(petalViewManager.PlayDisappearAndRelease(disappearingPositions, disappearDuration, accessKeys), petalViewManager.PlayAboutToExecuteShake(triggeredSkillPositions, accessKeys), tileViewManager.PlayTileChanges(changes));
        }
    }
}
