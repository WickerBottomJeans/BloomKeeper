using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Boosters
{
    public class GardenersGlovePresenter : BoosterRepresentationPresenter<GardenersGloveRepresentationData>
    {
        private const string SelectionViewUserName = nameof(GardenersGlovePresenter);

        private readonly PieceViewManager pieceViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVisual _boardVisual;
        private readonly PieceViewScope selectionViews;

        public override BoosterType BoosterType => DefaultNamespace.BoosterType.GardenersGlove;

        public GardenersGlovePresenter(PieceViewManager pieceViewManager, TileViewManager tileViewManager, BoardVisual boardVisual)
        {
            this.pieceViewManager = pieceViewManager;
            this.tileViewManager = tileViewManager;
            this._boardVisual = boardVisual;
            selectionViews = new PieceViewScope(pieceViewManager, SelectionViewUserName);
        }

        public override void ShowTargets(IReadOnlyList<Vector2Int> positions, BoosterTargetPresentationConfig.BoosterTargetMaterialMapping presentation)
        {
            tileViewManager.ShowBoosterTargets(positions, presentation.Material);
        }

        public override void HideTargets()
        {
            try
            {
                ClearSelectedTargets();
            }
            finally
            {
                tileViewManager.HideBoosterTargets();
            }
        }

        public override void SetTargetSelected(Vector2Int position, bool isSelected)
        {
            if (!isSelected)
            {
                DeselectTarget(position);
                return;
            }

            if (selectionViews.Owns(position)) throw new InvalidOperationException($"Gardener's Glove target at {position} is already selected.");
            selectionViews.AcquireRequired(position);
            try
            {
                pieceViewManager.ShowBoosterSelection(position, selectionViews);
            }
            catch
            {
                selectionViews.Release(position);
                throw;
            }
        }

        private void DeselectTarget(Vector2Int position)
        {
            if (!selectionViews.Owns(position)) throw new InvalidOperationException($"Gardener's Glove target at {position} is not selected.");

            pieceViewManager.HideBoosterSelection(position, selectionViews);
            selectionViews.Release(position);
        }

        protected override UniTask Play(GardenersGloveRepresentationData representation)
        {
            ClearSelectedTargets();
            return _boardVisual.PlaySwap(representation.OriginPosition, representation.TargetPosition);
        }

        private void ClearSelectedTargets()
        {
            var selectedPositions = new List<Vector2Int>(selectionViews.Keys);
            foreach (Vector2Int position in selectedPositions)
                DeselectTarget(position);
        }
    }
}
