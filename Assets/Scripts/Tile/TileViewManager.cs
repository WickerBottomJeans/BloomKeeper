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

        public void Init(Tile[,] tiles, BoardLayout layout)
        {
            int cols = tiles.GetLength(0);
            int rows = tiles.GetLength(1);
            _views = new TileView[cols, rows];

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    Vector3 worldPos = layout.GetCellWorldPos(col, row);
                    TileView view = Instantiate(_tileViewPrefab, worldPos, Quaternion.identity, transform);
                    view.Init(layout.CellSize);
                    _views[col, row] = view;
                    RefreshView(col, row, tiles[col, row]);
                }
            }
        }

        public void RefreshOverlay(int col, int row, Tile tile)
        {
            string overlayKey = tile.GetOverlaySpriteKey();
            if (overlayKey != null)
                _views[col, row].SetOverlay(SpriteLoader.Instance.GetSprite(overlayKey));
            else
                _views[col, row].ClearOverlay();
        }

        public void RefreshBase(int col, int row, Tile tile)
        {
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
        /// <param name="result"></param>
        /// <param name="grid"></param>
        public async UniTask OnMatchResolved(MatchResolveResult result, Tile[,] grid)
        {
            var transitionTasks = new List<UniTask>();

            foreach (Vector2Int pos in result.ChangedTiles)
            {
                TileView view = _views[pos.x, pos.y];
                Tile tile = grid[pos.x, pos.y];
                string newOverlayKey = tile.GetOverlaySpriteKey();

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