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

                if (view.OverlayRenderer.sprite != null && newOverlayKey != null)
                {
                    Sprite incoming = SpriteLoader.Instance.GetSprite(newOverlayKey);
                    transitionTasks.Add(TileViewAnimator.PlayOverlayTransition(view, incoming));
                }
                else if (view.OverlayRenderer.sprite != null && newOverlayKey == null)
                {
                    transitionTasks.Add(TileViewAnimator.PlayOverlayDespawn(view));
                }
                else if (view.OverlayRenderer.sprite == null && newOverlayKey != null)
                {
                    view.SetOverlay(SpriteLoader.Instance.GetSprite(newOverlayKey));
                    transitionTasks.Add(TileViewAnimator.PlayOverlaySpawn(view));
                }
            }

            await UniTask.WhenAll(transitionTasks);
        }
    }
}
