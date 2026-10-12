using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>Presents recorded deliveries through the shared piece-view lifecycle.</summary>
    public sealed class EggCollectionPresenter
    {
        private readonly PieceViewManager pieceViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardLayout boardLayout;

        public EggCollectionPresenter(PieceViewManager pieceViewManager, TileViewManager tileViewManager, BoardLayout boardLayout)
        {
            this.pieceViewManager = pieceViewManager;
            this.tileViewManager = tileViewManager;
            this.boardLayout = boardLayout;
        }

        public async UniTask Play(IReadOnlyList<EggCollection> collections, ResolutionPlayback playback)
        {
            using var views = new PieceViewScope(pieceViewManager, nameof(EggCollectionPresenter));
            foreach (EggCollection collection in collections) views.AcquireRequired(collection.EggPosition);
            var flights = new List<UniTask>();
            foreach (EggCollection collection in collections) flights.Add(PlayFlight(collection, views, playback));
            await UniTask.WhenAll(flights);
            playback.CancellationToken.ThrowIfCancellationRequested();

            var baskets = new HashSet<Vector2Int>();
            var feedback = new List<UniTask>();
            foreach (EggCollection collection in collections)
                if (baskets.Add(collection.BasketPosition)) feedback.Add(tileViewManager.PlayBasketReceive(collection.BasketPosition, playback.CancellationToken));
            await UniTask.WhenAll(feedback);
        }

        private async UniTask PlayFlight(EggCollection collection, PieceViewScope views, ResolutionPlayback playback)
        {
            try
            {
                EggView view = pieceViewManager.GetRequiredView<EggView>(views, collection.EggPosition);
                Vector3 destination = tileViewManager.GetBasketReceivingPosition(collection.BasketPosition);
                await view.PlayCollection(destination, boardLayout.TileSize, playback.CancellationToken);
                playback.HandleTileChangePresented(collection.Removal);
            }
            catch (System.Exception exception)
            {
                playback.FailPresentation(exception);
            }
            finally
            {
                // Recycling stops tweens before ownership is returned to the pool.
                pieceViewManager.RecycleView(collection.EggPosition, views);
            }
        }
    }
}
