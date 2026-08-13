using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using Petals;
using UnityEngine;

namespace Boosters
{
    public class GardenersGlovePresenter : BoosterRepresentationPresenter<GardenersGloveRepresentationData>
    {
        private const string SelectionViewUserName = nameof(GardenersGlovePresenter);

        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardActionCoordinator boardActionCoordinator;
        private readonly Dictionary<Vector2Int, ViewAccessKey> selectionAccessKeys = new Dictionary<Vector2Int, ViewAccessKey>();

        public override BoosterType BoosterType => DefaultNamespace.BoosterType.GardenersGlove;

        public GardenersGlovePresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardActionCoordinator boardActionCoordinator)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardActionCoordinator = boardActionCoordinator;
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

            if (selectionAccessKeys.ContainsKey(position)) throw new InvalidOperationException($"Gardener's Glove target at {position} is already selected.");
            if (!petalViewManager.TryAcquireView(position, SelectionViewUserName, out ViewAccessKey accessKey)) throw new InvalidOperationException($"Gardener's Glove could not acquire the petal view at {position}.");

            selectionAccessKeys.Add(position, accessKey);
            try
            {
                petalViewManager.ShowBoosterSelection(position, selectionAccessKeys);
            }
            catch
            {
                selectionAccessKeys.Remove(position);
                petalViewManager.ReleaseView(accessKey);
                throw;
            }
        }

        private void DeselectTarget(Vector2Int position)
        {
            if (!selectionAccessKeys.TryGetValue(position, out ViewAccessKey accessKey)) throw new InvalidOperationException($"Gardener's Glove target at {position} is not selected.");

            petalViewManager.HideBoosterSelection(position, selectionAccessKeys);
            petalViewManager.ReleaseView(accessKey);
            selectionAccessKeys.Remove(position);
        }

        protected override UniTask Play(GardenersGloveRepresentationData representation)
        {
            ClearSelectedTargets();
            return boardActionCoordinator.PlaySwap(representation.OriginPosition, representation.TargetPosition);
        }

        private void ClearSelectedTargets()
        {
            var selectedPositions = new List<Vector2Int>(selectionAccessKeys.Keys);
            foreach (Vector2Int position in selectedPositions)
                DeselectTarget(position);
        }
    }
}
