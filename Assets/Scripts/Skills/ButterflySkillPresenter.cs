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
        private readonly BoardCell[,] grid;
        private readonly float flightDuration;
        private readonly float disappearDuration;

        public ButterflySkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardLayout layout, BoardCell[,] grid, float flightDuration, float disappearDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.layout = layout;
            this.grid = grid;
            this.flightDuration = flightDuration;
            this.disappearDuration = disappearDuration;
        }

        protected override async UniTask Play(ButterflyRepresentationData representation, MatchGroupResolveResult resolution)
        {
            var changedPositions = new List<Vector2Int>(SkillPresentationImpactQueries.GetChangedPositions(resolution));
            if (!representation.Target.HasValue)
            {
                await UniTask.WhenAll(petalViewManager.PlayDisappearAndRelease(new[] { representation.Source }, disappearDuration), tileViewManager.PlayTileChanges(changedPositions, grid));
                return;
            }

            await petalViewManager.PlayFly(representation.Source, representation.Target.Value, layout, flightDuration);
            var disappearingPositions = new List<Vector2Int> { representation.Source };
            var triggeredSkillPositions = new List<Vector2Int>();
            if (SkillPresentationImpactQueries.WasPetalRemovedAt(resolution, representation.Target.Value) && representation.Target.Value != representation.Source && !resolution.IsTriggeredSkillPosition(representation.Target.Value))
                disappearingPositions.Add(representation.Target.Value);
            if (resolution.IsTriggeredSkillPosition(representation.Target.Value) && representation.Target.Value != representation.Source)
                triggeredSkillPositions.Add(representation.Target.Value);

            await UniTask.WhenAll(petalViewManager.PlayDisappearAndRelease(disappearingPositions, disappearDuration), petalViewManager.PlayAboutToExecuteShake(triggeredSkillPositions), tileViewManager.PlayTileChanges(changedPositions, grid));
        }
    }
}
