using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Boosters
{
    /// <summary>Owns booster selection and pending approval, including approval/rejection guards. GameBoard only gates board availability.</summary>
    public sealed class BoardBoosterInteraction : IDisposable
    {
        private readonly Tile[,] boardGrid;
        private readonly BoardInputHandler boardInputHandler;
        private readonly BoardVisual boardVisual;
        private IBoosterChooser activeBoosterChooser;
        private BoosterType? pendingBoosterType;
        private bool isDisposed;

        public BoardBoosterInteraction(Tile[,] boardGrid, BoardInputHandler boardInputHandler, BoardVisual boardVisual)
        {
            this.boardGrid = boardGrid;
            this.boardInputHandler = boardInputHandler;
            this.boardVisual = boardVisual;
        }

        public async UniTask<BoosterTargetSelectionResult> Select(BoosterType boosterType)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(BoardBoosterInteraction));
            if (activeBoosterChooser != null || pendingBoosterType.HasValue) throw new InvalidOperationException("A booster interaction is already active.");
            IBoosterChooser boosterChooser = BoosterManager.CreateChooser(boosterType);
            activeBoosterChooser = boosterChooser;
            bool boosterTargetsShown = false;
            boosterChooser.TargetSelectionChanged += boardVisual.SetBoosterTargetSelected;
            try
            {
                boardVisual.ShowBoosterTargets(boosterType, boosterChooser.GetBoosterTargetCandidates(boardGrid));
                boosterTargetsShown = true;
                BoosterTargetSelectionResult targetSelectionResult = await boosterChooser.Choose(boardGrid, boardInputHandler);
                if (isDisposed) throw new OperationCanceledException();
                pendingBoosterType = targetSelectionResult.IsCanceled || targetSelectionResult.IsUnavailable ? null : boosterType;
                return targetSelectionResult;
            }
            finally
            {
                activeBoosterChooser = null;
                boosterChooser.TargetSelectionChanged -= boardVisual.SetBoosterTargetSelected;
                if (boosterTargetsShown) boardVisual.HideBoosterTargets();
            }
        }

        public void Cancel() => activeBoosterChooser?.Cancel();

        public void ValidateApproval(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
        {
            if (pendingBoosterType != boosterType) throw new InvalidOperationException("Approved booster does not match the pending booster type.");
            if (targets == null) throw new ArgumentNullException(nameof(targets));
        }

        public void Complete()
        {
            if (!pendingBoosterType.HasValue) throw new InvalidOperationException("There is no booster awaiting authorization.");
            pendingBoosterType = null;
        }

        public void Dispose()
        {
            isDisposed = true;
            pendingBoosterType = null;
            Cancel();
        }
    }
}
