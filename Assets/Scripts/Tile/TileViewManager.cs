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

        public void Init(BoardCell[,] grid, BoardLayout layout)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            _views = new TileView[cols, rows];

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    BoardCell cell = grid[col, row];
                    if (cell.IsVoid) continue;

                    Vector3 worldPos = layout.GetCellWorldPos(col, row);
                    TileView view = Instantiate(_tileViewPrefab, worldPos, Quaternion.identity, transform);
                    view.Init(layout.CellSize);
                    _views[col, row] = view;
                    RefreshView(col, row, cell);
                }
            }
        }

        public void RefreshOverlay(int col, int row, BoardCell cell)
        {
            if (_views[col, row] == null || cell.IsVoid) return;

            string overlayKey = cell.GetOverlaySpriteKey();
            if (overlayKey != null)
                _views[col, row].SetOverlay(SpriteLoader.Instance.GetSprite(overlayKey));
            else
                _views[col, row].ClearOverlay();
        }

        public void RefreshBase(int col, int row, BoardCell cell)
        {
            if (_views[col, row] == null || cell.IsVoid) return;

            string key = SpriteKeyHelper.GetTileSpriteKey(cell.Tile.TileType);
            Sprite sprite = SpriteLoader.Instance.GetSprite(key);
            _views[col, row].SetBase(sprite);
        }
        
        /// <summary>
        /// Refresh both base tile and its overlay
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="cell"></param>
        private void RefreshView(int col, int row, BoardCell cell)
        {
            RefreshBase(col, row, cell);
            RefreshOverlay(col, row, cell);
        }
        
        /// <summary>
        /// Refreshes tile views for all tiles that changed state during match resolution
        /// </summary>
        /// <param name="changedTiles"></param>
        /// <param name="grid"></param>
        public async UniTask PlayTileChanges(List<Vector2Int> changedTiles, BoardCell[,] grid)
        {
            var transitionTasks = new List<UniTask>();

            foreach (Vector2Int pos in changedTiles)
            {
                TileView view = _views[pos.x, pos.y];
                BoardCell cell = grid[pos.x, pos.y];
                if (view == null || cell.IsVoid) continue;

                string newOverlayKey = cell.GetOverlaySpriteKey();

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
