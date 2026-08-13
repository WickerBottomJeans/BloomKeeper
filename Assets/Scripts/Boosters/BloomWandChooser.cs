using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public class BloomWandChooser : IBoosterChooser
    {
        private Tile[,] grid;
        private UniTaskCompletionSource<BoosterTargetSelectionResult> completionSource;
        private Vector2Int? pressedTarget;

        public event Action<Vector2Int, bool> TargetSelectionChanged;

        public IReadOnlyList<Vector2Int> GetBoosterTargetCandidates(Tile[,] grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            var candidates = new List<Vector2Int>();
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    if (CanTarget(grid, new Vector2Int(x, y)))
                        candidates.Add(new Vector2Int(x, y));

            return candidates;
        }

        public async UniTask<BoosterTargetSelectionResult> Choose(Tile[,] grid, BoardInputHandler inputHandler)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (inputHandler == null) throw new ArgumentNullException(nameof(inputHandler));
            if (this.grid != null) throw new InvalidOperationException("Bloom Wand target selection is already active.");

            this.grid = grid;
            completionSource = new UniTaskCompletionSource<BoosterTargetSelectionResult>();
            pressedTarget = null;

            inputHandler.OnPointerPressed += HandlePointerPressed;
            inputHandler.OnPointerReleased += HandlePointerReleased;
            inputHandler.OnPointerCanceled += Cancel;

            try
            {
                return await completionSource.Task;
            }
            finally
            {
                inputHandler.OnPointerPressed -= HandlePointerPressed;
                inputHandler.OnPointerReleased -= HandlePointerReleased;
                inputHandler.OnPointerCanceled -= Cancel;
                completionSource = null;
                this.grid = null;
                pressedTarget = null;
            }
        }

        public void Cancel()
        {
            EnsureActive();
            completionSource.TrySetResult(BoosterTargetSelectionResult.Canceled);
        }

        private void HandlePointerPressed(Vector2Int position)
        {
            EnsureActive();
            pressedTarget = CanTarget(position) ? position : null;
        }

        private void HandlePointerReleased(Vector2Int position)
        {
            EnsureActive();

            Vector2Int? target = pressedTarget;
            pressedTarget = null;
            if (!target.HasValue || target.Value != position || !CanTarget(position)) return;

            TargetSelectionChanged?.Invoke(position, true);
            completionSource.TrySetResult(BoosterTargetSelectionResult.Selected(new[] { position }));
        }

        private bool CanTarget(Vector2Int position)
        {
            return CanTarget(grid, position);
        }

        private  bool CanTarget(Tile[,] grid, Vector2Int position)
        {
            if (position.x < 0 || position.x >= grid.GetLength(0) || position.y < 0 || position.y >= grid.GetLength(1)) return false;

            Tile tile = grid[position.x, position.y];
            return tile != null && tile.GetClearEffectCapacity() > 0;
        }

        private void EnsureActive()
        {
            if (completionSource == null) throw new InvalidOperationException("Bloom Wand target selection has not begun.");
        }
    }
}
