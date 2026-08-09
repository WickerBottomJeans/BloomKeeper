using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;

namespace DefaultNamespace
{
    public class TileViewManager : MonoBehaviour
    {
        [SerializeField] private TileView _tileViewPrefab;

        private TileView[,] _views;
        private readonly List<TileView> _activeBoosterTargetViews = new List<TileView>();
        private bool _boosterTargetsShown;

        public void Init(Tile[,] grid, BoardLayout layout)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            _views = new TileView[cols, rows];

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    Tile tile = grid[col, row];
                    if (tile == null) continue;

                    Vector3 worldPos = layout.GetTileWorldPos(col, row);
                    TileView view = Instantiate(_tileViewPrefab, worldPos, Quaternion.identity, transform);
                    view.Init(layout.TileSize, -row);
                    _views[col, row] = view;
                    RefreshView(col, row, tile);
                }
            }
        }

        public void RefreshOverlay(int col, int row, Tile tile)
        {
            if (_views[col, row] == null || tile == null) return;

            string overlayKey = SpriteKeyHelper.GetTileOverlayKey(tile.TileType, tile.ObstacleLayerCount);
            if (overlayKey != null)
                _views[col, row].SetOverlay(SpriteLoader.Instance.GetSprite(overlayKey));
            else
                _views[col, row].ClearOverlay();
        }

        public void RefreshBase(int col, int row, Tile tile)
        {
            if (_views[col, row] == null || tile == null) return;

            string key = SpriteKeyHelper.GetTileSpriteKey(tile.TileType);
            Sprite sprite = SpriteLoader.Instance.GetSprite(key);
            _views[col, row].SetBase(sprite);
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
        /// Refresh both base tile and its overlay
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="tile"></param>
        private void RefreshView(int col, int row, Tile tile)
        {
            RefreshBase(col, row, tile);
            RefreshOverlay(col, row, tile);
        }
        
        /// <summary>
        /// Refreshes tile views for all tiles that changed state during match resolution
        /// </summary>
        /// <param name="changes"></param>
        public async UniTask PlayTileChanges(IReadOnlyList<TileChange> changes)
        {
            var transitionTasks = new List<UniTask>();

            foreach (TileChange change in changes)
            {
                Vector2Int pos = change.Position;
                TileView view = _views[pos.x, pos.y];
                if (view == null || change.After.IsVoid) continue;

                string newOverlayKey = SpriteKeyHelper.GetTileOverlayKey(change.After.TileType.Value, change.After.ObstacleLayerCount);

                if (view.HasOverlay && newOverlayKey != null)
                {
                    Sprite incoming = SpriteLoader.Instance.GetSprite(newOverlayKey);
                    transitionTasks.Add(view.PlayOverlayTransition(incoming));
                }
                else if (view.HasOverlay && newOverlayKey == null)
                {
                    transitionTasks.Add(view.PlayOverlayDespawn());
                }
                else if (!view.HasOverlay && newOverlayKey != null)
                {
                    view.SetOverlay(SpriteLoader.Instance.GetSprite(newOverlayKey));
                    transitionTasks.Add(view.PlayOverlaySpawn());
                }
            }

            await UniTask.WhenAll(transitionTasks);
        }
    }
}
