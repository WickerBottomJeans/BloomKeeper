using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public class GardenersGloveChooser : IBoosterChooser
    {
        private Tile[,] grid;
        private UniTaskCompletionSource<BoosterTargetSelectionResult> completionSource;
        private readonly List<Vector2Int> selectedTargets = new List<Vector2Int>(2);
        private Vector2Int? pressedTarget;
        private bool isCompleting;

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
            if (this.grid != null) throw new InvalidOperationException("Gardener's Glove target selection is already active.");

            this.grid = grid;
            completionSource = new UniTaskCompletionSource<BoosterTargetSelectionResult>();
            selectedTargets.Clear();
            pressedTarget = null;
            isCompleting = false;

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
                selectedTargets.Clear();
                pressedTarget = null;
                isCompleting = false;
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
            if (isCompleting) return;
            pressedTarget = CanTarget(position) ? position : null;
        }

        private void HandlePointerReleased(Vector2Int position)
        {
            EnsureActive();
            if (isCompleting) return;

            Vector2Int? target = pressedTarget;
            pressedTarget = null;
            if (!target.HasValue || target.Value != position || !CanTarget(position)) return;

            if (selectedTargets.Remove(position))
            {
                TargetSelectionChanged?.Invoke(position, false);
                return;
            }

            selectedTargets.Add(position);
            TargetSelectionChanged?.Invoke(position, true);

            if (selectedTargets.Count == 2)
            {
                isCompleting = true;
                CompleteAfterSelectionFrame().Forget();
            }
        }

        private async UniTask CompleteAfterSelectionFrame()
        {
            UniTaskCompletionSource<BoosterTargetSelectionResult> activeCompletionSource = completionSource;
            IReadOnlyList<Vector2Int> targets = selectedTargets.ToArray();
            await UniTask.NextFrame();
            activeCompletionSource.TrySetResult(BoosterTargetSelectionResult.Selected(targets));
        }

        private bool CanTarget(Vector2Int position)
        {
            return CanTarget(grid, position);
        }

        private  bool CanTarget(Tile[,] grid, Vector2Int position)
        {
            if (position.x < 0 || position.x >= grid.GetLength(0) || position.y < 0 || position.y >= grid.GetLength(1)) return false;

            Tile tile = grid[position.x, position.y];
            return tile != null && tile.CanSwapPetal();
        }

        private void EnsureActive()
        {
            if (completionSource == null) throw new InvalidOperationException("Gardener's Glove target selection has not begun.");
        }
    }
}
