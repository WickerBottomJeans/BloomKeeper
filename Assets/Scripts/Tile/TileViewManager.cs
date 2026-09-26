using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class TileViewManager : MonoBehaviour
    {
        [SerializeField] private TileView _tileViewPrefab;
        [SerializeField] private TileFeatureView[] tileFeaturePrefabs;

        private TileView[,] _views;
        private TileFeatureState[,] displayedFeatureStates;
        private readonly Dictionary<TileFeatureType, TileFeatureView> tileFeaturePrefabsByType = new();
        private readonly Dictionary<Vector2Int, List<(TileChange tileChange, UniTaskCompletionSource completionSource)>> pendingFeatureChanges = new();
        private readonly HashSet<Vector2Int> animatingFeaturePositions = new();
        private Exception featurePresentationFailure;
        private readonly List<TileView> _activeBoosterTargetViews = new List<TileView>();
        private bool _boosterTargetsShown;

        #region Unity Lifecycle
        private void OnDestroy()
        {
            FailPendingFeatureChanges(new OperationCanceledException("The board was destroyed during feature presentation."));
        }
        #endregion

        #region Public API
        public void Init(Tile[,] grid, BoardLayout layout)
        {
            foreach (TileFeatureView tileFeaturePrefab in tileFeaturePrefabs)
            {
                if (!Enum.IsDefined(typeof(TileFeatureType), tileFeaturePrefab.FeatureType)) throw new InvalidOperationException($"Unknown feature prefab type: {tileFeaturePrefab.FeatureType}.");
                tileFeaturePrefabsByType.Add(tileFeaturePrefab.FeatureType, tileFeaturePrefab);
            }

            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            _views = new TileView[cols, rows];
            displayedFeatureStates = new TileFeatureState[cols, rows];

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    Tile tile = grid[col, row];
                    if (tile == null) continue;

                    Vector3 worldPos = layout.GetTileWorldPos(col, row);
                    TileView view = Instantiate(_tileViewPrefab, worldPos, Quaternion.identity, transform);
                    TileFeatureState tileFeatureState = tile.Feature?.CaptureFeatureState();
                    view.Init(layout.TileSize, -row, tile.IsPlayable, tileFeatureState, tileFeaturePrefabsByType);
                    _views[col, row] = view;
                    displayedFeatureStates[col, row] = tileFeatureState;
                }
            }
        }

        public void ShowBoosterTargets(IReadOnlyList<Vector2Int> positions, Material material)
        {
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (_boosterTargetsShown) throw new InvalidOperationException("Booster targets are already shown.");

            var views = new List<TileView>(positions.Count);
            var uniqueViews = new HashSet<TileView>();
            foreach (Vector2Int position in positions)
            {
                if (position.x < 0 || position.x >= _views.GetLength(0) || position.y < 0 || position.y >= _views.GetLength(1))
                    throw new ArgumentOutOfRangeException(nameof(positions), position, "Booster target position is outside the board view.");

                TileView view = _views[position.x, position.y];
                if (view == null) throw new InvalidOperationException($"Booster target position {position} has no tile view.");
                if (!uniqueViews.Add(view)) throw new ArgumentException($"Booster target position {position} is duplicated.", nameof(positions));
                views.Add(view);
            }

            _boosterTargetsShown = true;
            try
            {
                foreach (TileView view in views)
                {
                    view.ShowBoosterTarget(material);
                    _activeBoosterTargetViews.Add(view);
                }
            }
            catch
            {
                foreach (TileView view in _activeBoosterTargetViews)
                    view.HideBoosterTarget();
                _activeBoosterTargetViews.Clear();
                _boosterTargetsShown = false;
                throw;
            }
        }

        public void HideBoosterTargets()
        {
            if (!_boosterTargetsShown) throw new InvalidOperationException("Booster targets are not shown.");

            foreach (TileView view in _activeBoosterTargetViews)
                view.HideBoosterTarget();
            _activeBoosterTargetViews.Clear();
            _boosterTargetsShown = false;
        }

        public void PlayRipple(Vector2 worldOrigin, float strength, float radius, float travelDuration, float tileMoveDuration)
        {
            if (strength < 0f) throw new ArgumentOutOfRangeException(nameof(strength));
            if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
            if (travelDuration < 0f) throw new ArgumentOutOfRangeException(nameof(travelDuration));
            if (tileMoveDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(tileMoveDuration));

            foreach (TileView view in _views)
            {
                if (view == null) continue;

                Vector2 fromOrigin = (Vector2)view.transform.position - worldOrigin;
                float distance = fromOrigin.magnitude;
                if (distance > radius) continue;

                float normalizedDistance = distance / radius;
                Vector2 direction = distance > Mathf.Epsilon ? fromOrigin / distance : Vector2.zero;
                Vector3 worldDisplacement = direction * (strength * (1f - normalizedDistance));
                Vector2 displacement = transform.InverseTransformVector(worldDisplacement);
                float delay = normalizedDistance * travelDuration;
                view.PlayRipple(displacement, delay, tileMoveDuration).Forget();
            }
        }
        
        /// <summary>
        /// Plays feature changes in snapshot order on each tile.
        /// </summary>
        /// <param name="changes"></param>
        public async UniTask PlayTileChanges(IReadOnlyList<TileChange> changes)
        {
            if (featurePresentationFailure != null) throw new InvalidOperationException("Feature presentation has already failed.", featurePresentationFailure);
            var transitionTasks = new List<UniTask>();
            var changedPositions = new HashSet<Vector2Int>();

            foreach (TileChange change in changes)
            {
                if (!change.FeatureChanged) continue;
                Vector2Int position = change.Position;
                if (_views[position.x, position.y] == null || change.After.IsVoid) throw new InvalidOperationException($"Feature change at {position} requires a tile view.");
                if (!pendingFeatureChanges.TryGetValue(position, out var tileFeatureChanges))
                {
                    tileFeatureChanges = new List<(TileChange, UniTaskCompletionSource)>();
                    pendingFeatureChanges.Add(position, tileFeatureChanges);
                }

                var completionSource = new UniTaskCompletionSource();
                tileFeatureChanges.Add((change, completionSource));
                transitionTasks.Add(completionSource.Task);
                changedPositions.Add(position);
            }

            foreach (Vector2Int position in changedPositions)
                PlayPendingTileFeatureChanges(position).Forget();

            await UniTask.WhenAll(transitionTasks);
        }
        #endregion

        #region Private Methods
        private async UniTask PlayPendingTileFeatureChanges(Vector2Int position)
        {
            if (featurePresentationFailure != null || !animatingFeaturePositions.Add(position)) return;

            try
            {
                List<(TileChange tileChange, UniTaskCompletionSource completionSource)> tileFeatureChanges = pendingFeatureChanges[position];
                while (tileFeatureChanges.Count > 0)
                {
                    TileFeatureState displayedFeatureState = displayedFeatureStates[position.x, position.y];
                    int nextChangeIndex = tileFeatureChanges.FindIndex(change => TileFeatureState.AreFeatureStatesEqual(displayedFeatureState, change.tileChange.Before.FeatureState));

                    // Wait for the earlier impact to arrive.
                    if (nextChangeIndex < 0) return;

                    var nextChange = tileFeatureChanges[nextChangeIndex];
                    await _views[position.x, position.y].PlayTileFeatureChange(nextChange.tileChange.Before.FeatureState, nextChange.tileChange.After.FeatureState);
                    if (featurePresentationFailure != null) return;
                    displayedFeatureStates[position.x, position.y] = nextChange.tileChange.After.FeatureState;
                    tileFeatureChanges.RemoveAt(nextChangeIndex);
                    nextChange.completionSource.TrySetResult();
                }

                pendingFeatureChanges.Remove(position);
            }
            catch (Exception exception)
            {
                FailPendingFeatureChanges(exception);
            }
            finally
            {
                animatingFeaturePositions.Remove(position);
            }
        }

        private void FailPendingFeatureChanges(Exception exception)
        {
            featurePresentationFailure = exception;
            var completionSources = new List<UniTaskCompletionSource>();
            foreach (var tileFeatureChanges in pendingFeatureChanges.Values)
            foreach (var tileFeatureChange in tileFeatureChanges)
                completionSources.Add(tileFeatureChange.completionSource);
            pendingFeatureChanges.Clear();

            foreach (UniTaskCompletionSource completionSource in completionSources)
            {
                if (exception is OperationCanceledException cancellationException)
                    completionSource.TrySetCanceled(cancellationException.CancellationToken);
                else
                    completionSource.TrySetException(exception);
            }
        }
        #endregion
    }
}
