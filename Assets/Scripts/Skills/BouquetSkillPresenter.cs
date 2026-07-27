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
        private readonly BoardCell[,] grid;
        private readonly float disappearDuration;

        public BouquetSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardCell[,] grid, float disappearDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.grid = grid;
            this.disappearDuration = disappearDuration;
        }

        protected override async UniTask Play(BouquetRepresentationData representation, MatchGroupResolveResult resolution)
        {
            HashSet<Vector2Int> removedPositions = SkillPresentationImpactQueries.GetCurrentSkillConsumedPositions(resolution);
            removedPositions.Add(representation.Center);
            var changedPositions = new List<Vector2Int>(SkillPresentationImpactQueries.GetChangedPositions(resolution));
            UniTask disappearTask = petalViewManager.PlayDisappearAndRelease(new List<Vector2Int>(removedPositions), disappearDuration);
            UniTask triggeredSkillTask = petalViewManager.PlayAboutToExecuteShake(resolution.TriggeredSkillPositions);
            UniTask bloomTask = boardVFXManager.PlayBouquetBloomVFX(representation.Center);
            UniTask tileTask = tileViewManager.PlayTileChanges(changedPositions, grid);
            await UniTask.WhenAll(disappearTask, triggeredSkillTask, bloomTask, tileTask);
        }
    }
}
