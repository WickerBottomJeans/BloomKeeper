using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class BoardActionCoordinator
    {
        private readonly PetalViewManager petalViewManager;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout layout;
        private readonly Tile[,] grid;

        public BoardActionCoordinator(PetalViewManager petalViewManager, BoardAudioManager boardAudioManager, BoardLayout layout, Tile[,] grid)
        {
            this.petalViewManager = petalViewManager;
            this.boardAudioManager = boardAudioManager;
            this.layout = layout;
            this.grid = grid;
        }

        public async UniTask PlaySwap(Vector2Int tileA, Vector2Int tileB)
        {
            boardAudioManager.PlayPetalSwap();
            await PlaySwap(tileA, tileB, layout.TileSize);
        }

        public async UniTask PlayInvalidSwapBack(Vector2Int tileA, Vector2Int tileB)
        {
            boardAudioManager.PlayInvalidSwap();
            await PlaySwap(tileA, tileB, layout.TileSize);
        }

        public async UniTask PlayGravity(List<(Vector2Int from, Vector2Int to)> moves)
        {
            var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
            try
            {
                foreach (var (from, _) in moves)
                    AcquireRequiredView(from, accessKeys);

                await petalViewManager.OnGravityApplied(moves, layout, accessKeys);

                if (moves.Count > 0)
                    boardAudioManager.PlayPetalLanding();
            }
            finally
            {
                ReleaseRemainingViews(accessKeys);
            }
        }

        public async UniTask PlayFill(List<Vector2Int> filledTiles)
        {
            var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
            try
            {
                if (filledTiles.Count > 0)
                    boardAudioManager.PlayPetalSpawning();

                await petalViewManager.OnFilled(filledTiles, layout, grid, nameof(BoardActionCoordinator), accessKeys);
            }
            finally
            {
                ReleaseRemainingViews(accessKeys);
            }
        }

        public async UniTask PlayShuffle(List<Vector2Int> tiles)
        {
            var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
            try
            {
                foreach (Vector2Int tile in tiles)
                    AcquireRequiredView(tile, accessKeys);

                if (tiles.Count > 0)
                    boardAudioManager.PlayBoardShuffle();

                await petalViewManager.OnShuffled(tiles, layout, grid, nameof(BoardActionCoordinator), accessKeys);
            }
            finally
            {
                ReleaseRemainingViews(accessKeys);
            }
        }

        public void RefreshTile(Vector2Int tile, Petal petal)
        {
            var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
            try
            {
                if (petalViewManager.TryAcquireView(tile, nameof(BoardActionCoordinator), out ViewAccessKey accessKey))
                    accessKeys.Add(tile, accessKey);

                petalViewManager.RefreshTile(tile, petal, layout, nameof(BoardActionCoordinator), accessKeys);
            }
            finally
            {
                ReleaseRemainingViews(accessKeys);
            }
        }

        private async UniTask PlaySwap(Vector2Int tileA, Vector2Int tileB, float tileSize)
        {
            var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
            try
            {
                AcquireRequiredView(tileA, accessKeys);
                AcquireRequiredView(tileB, accessKeys);
                await petalViewManager.OnSwap(tileA, tileB, tileSize, accessKeys);
            }
            finally
            {
                ReleaseRemainingViews(accessKeys);
            }
        }

        private void AcquireRequiredView(Vector2Int position, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            if (accessKeys.ContainsKey(position)) return;
            if (!petalViewManager.TryAcquireView(position, nameof(BoardActionCoordinator), out ViewAccessKey accessKey))
                throw new InvalidOperationException($"Petal view at {position} cannot be acquired by {nameof(BoardActionCoordinator)}.");
            accessKeys.Add(position, accessKey);
        }

        private void ReleaseRemainingViews(IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var remainingAccessKeys = new List<ViewAccessKey>(accessKeys.Values);
            foreach (ViewAccessKey accessKey in remainingAccessKeys)
                petalViewManager.ReleaseView(accessKey);
            accessKeys.Clear();
        }
    }
}
