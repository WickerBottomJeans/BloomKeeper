using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Boosters
{
    public sealed class BloomWandPresenter : BoosterRepresentationPresenter<BloomWandRepresentationData>
    {
        private const float RippleStrengthInTiles = 0.25f;
        private const float RippleRadiusInTiles = 6f;
        private const float RippleTravelDuration = 0.25f;
        private const float RippleTileMoveDuration = 0.22f;

        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardLayout boardLayout;

        public override BoosterType BoosterType => DefaultNamespace.BoosterType.BloomWand;

        public BloomWandPresenter(TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardLayout boardLayout)
        {
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.boardLayout = boardLayout;
        }

        public override void ShowTargets(IReadOnlyList<Vector2Int> positions, BoosterTargetPresentationConfig.BoosterTargetMaterialMapping presentation)
        {
            tileViewManager.ShowBoosterTargets(positions, presentation.Material);
        }

        public override void HideTargets()
        {
            tileViewManager.HideBoosterTargets();
        }

        protected override async UniTask Play(BloomWandRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            await boardVFXManager.PlayBloomWandUntilImpact(representation.TargetPosition);

            Vector2 impactWorldPosition = boardLayout.GetTileWorldPos(representation.TargetPosition.x, representation.TargetPosition.y);
            tileViewManager.PlayRipple(impactWorldPosition, RippleStrengthInTiles * boardLayout.TileSize, RippleRadiusInTiles * boardLayout.TileSize, RippleTravelDuration, RippleTileMoveDuration);
        }
    }
}
