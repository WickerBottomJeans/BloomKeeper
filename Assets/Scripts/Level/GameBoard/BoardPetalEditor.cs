using System;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>Owns the admin petal editor's selection, popup, and view refresh; delegates mutations to BoardLogic.</summary>
    public sealed class BoardPetalEditor : IDisposable
    {
        private readonly BoardLogic boardLogic;
        private readonly BoardLayout boardLayout;
        private readonly Camera camera;
        private readonly BoardInputHandler boardInputHandler;
        private readonly BoardVisual boardVisual;
        private readonly UIManager uiManager;
        private readonly Func<bool> canEditBoard;
        private Vector2Int editingTilePosition;

        public BoardPetalEditor(BoardLogic boardLogic, BoardLayout boardLayout, Camera camera, BoardInputHandler boardInputHandler, BoardVisual boardVisual, UIManager uiManager, Func<bool> canEditBoard)
        {
            this.boardLogic = boardLogic;
            this.boardLayout = boardLayout;
            this.camera = camera;
            this.boardInputHandler = boardInputHandler;
            this.boardVisual = boardVisual;
            this.uiManager = uiManager;
            this.canEditBoard = canEditBoard;
            boardInputHandler.OnEditRequested += Open;
            uiManager.OnPetalEditConfirmed += Apply;
        }

        private void Open(Vector2Int position)
        {
            if (!canEditBoard()) return;
            if (!boardLogic.CanEditPetal(position)) return;
            editingTilePosition = position;
            Vector2 worldPosition = boardLayout.GetTileWorldPosition(position.x, position.y);
            uiManager.ShowPetalEditorPopup(camera.WorldToScreenPoint(worldPosition));
        }

        private void Apply(PetalType petalType, SpecialSkillType skillType)
        {
            if (!canEditBoard()) return;
            Petal editedPetal = boardLogic.EditPetal(editingTilePosition, petalType, skillType);
            if (editedPetal == null) return;
            boardVisual.RefreshTile(editingTilePosition, editedPetal);
        }

        public void Dispose()
        {
            if (boardInputHandler != null) boardInputHandler.OnEditRequested -= Open;
            if (uiManager != null) uiManager.OnPetalEditConfirmed -= Apply;
        }
    }
}
