using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly List<TileView> _activeBoosterTargetViews = new List<TileView>();
        private bool _boosterTargetsShown;

        #region Public API
        public Vector3 GetBasketReceivingPosition(Vector2Int position) => _views[position.x, position.y].GetFeatureView<BasketFeatureView>().ReceivingPosition;
        public UniTask PlayBasketReceive(Vector2Int position, CancellationToken cancellationToken) => _views[position.x, position.y].GetFeatureView<BasketFeatureView>().PlayReceive(cancellationToken);

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

                    Vector3 worldPos = layout.GetTileWorldPosition(col, row);
                    TileView view = Instantiate(_tileViewPrefab, worldPos, Quaternion.identity, transform);
                    TileFeatureState tileFeatureState = tile.Feature?.CaptureFeatureState();
                    view.Init(layout.TileSize, -row, tileFeatureState, tileFeaturePrefabsByType);
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
        /// Plays one feature transition from the displayed state.
        /// </summary>
        public async UniTask PlayTileChange(TileChange tileChange, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!tileChange.FeatureChanged) throw new ArgumentException("A feature transition requires a changed feature.", nameof(tileChange));
            Vector2Int position = tileChange.Position;
            if (_views[position.x, position.y] == null || tileChange.After.IsVoid) throw new InvalidOperationException($"Feature change at {position} requires a tile view.");
            if (!TileFeatureState.AreFeatureStatesEqual(displayedFeatureStates[position.x, position.y], tileChange.Before.FeatureState))
                throw new InvalidOperationException($"Feature change at {position} does not follow the displayed state.");
            await _views[position.x, position.y].PlayTileFeatureChange(tileChange.Before.FeatureState, tileChange.After.FeatureState).AttachExternalCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            displayedFeatureStates[position.x, position.y] = tileChange.After.FeatureState;
        }
        #endregion
    }
}
