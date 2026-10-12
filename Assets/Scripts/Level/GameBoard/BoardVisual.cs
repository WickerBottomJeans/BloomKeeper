using System.Collections.Generic;
using System.Threading;
using Boosters;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DefaultNamespace.VFX;
using Skills;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class BoardVisual
    {
        private readonly PieceViewManager pieceViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout boardLayout;
        private readonly SkillAnimationLibrary skillAnimationLibrary;
        private readonly BoosterPresenter boosterPresenter;

        public BoardVisual(PieceViewManager pieceViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, SkillAnimationLibrary skillAnimationLibrary, BoosterPresenter boosterPresenter, BoardAudioManager boardAudioManager, BoardLayout boardLayout)
        {
            this.pieceViewManager = pieceViewManager;
            this.tileViewManager = tileViewManager;
            this.boardAudioManager = boardAudioManager;
            this.boardLayout = boardLayout;
            this.skillAnimationLibrary = skillAnimationLibrary;
            this.boosterPresenter = boosterPresenter;
            boosterPresenter.Init(pieceViewManager, tileViewManager, boardVFXManager, this, boardLayout);
        }

        public async UniTask PlayResolution(BoardResolution boardResolution, CancellationToken cancellationToken)
        {
            using var resolutionPlayback = new ResolutionPlayback(boardResolution, pieceViewManager, tileViewManager, boardAudioManager, skillAnimationLibrary, boardLayout, cancellationToken);
            await resolutionPlayback.Play();
        }

        public UniTask PlaySwap(Vector2Int fromPosition, Vector2Int toPosition)
        {
            boardAudioManager.PlayPetalSwap();
            return AnimateSwap(fromPosition, toPosition);
        }

        public UniTask PlayInvalidSwapBack(Vector2Int fromPosition, Vector2Int toPosition)
        {
            boardAudioManager.PlayInvalidSwap();
            return AnimateSwap(fromPosition, toPosition);
        }

        private async UniTask AnimateSwap(Vector2Int fromPosition, Vector2Int toPosition)
        {
            using var pieceViewScope = new PieceViewScope(pieceViewManager, nameof(PlaySwap));
            pieceViewScope.AcquireRequired(fromPosition);
            pieceViewScope.AcquireRequired(toPosition);
            await pieceViewManager.OnSwap(fromPosition, toPosition, boardLayout.TileSize, pieceViewScope);
        }

        public async UniTask PlayGravity(List<(Vector2Int fromPosition, Vector2Int toPosition)> gravityMoves)
        {
            using var pieceViewScope = new PieceViewScope(pieceViewManager, nameof(PlayGravity));
            foreach (var (fromPosition, _) in gravityMoves) pieceViewScope.AcquireRequired(fromPosition);
            await pieceViewManager.OnGravityApplied(gravityMoves, boardLayout, pieceViewScope);
            if (gravityMoves.Count > 0) boardAudioManager.PlayPetalLanding();
        }

        public async UniTask PlayFill(IReadOnlyList<(Vector2Int position, Petal newPetal)> filledPetals)
        {
            using var pieceViewScope = new PieceViewScope(pieceViewManager, nameof(PlayFill));
            if (filledPetals.Count > 0) boardAudioManager.PlayPetalSpawning();
            await PetalPresentation.OnFilled(pieceViewManager, filledPetals, boardLayout, pieceViewScope);
        }

        public async UniTask PlayShuffle(IReadOnlyList<(Vector2Int position, Petal newPetal)> shuffledPetals)
        {
            using var pieceViewScope = new PieceViewScope(pieceViewManager, nameof(PlayShuffle));
            foreach (var (position, _) in shuffledPetals) pieceViewScope.AcquireRequired(position);
            if (shuffledPetals.Count > 0) boardAudioManager.PlayBoardShuffle();
            await PetalPresentation.OnShuffled(pieceViewManager, shuffledPetals, boardLayout, pieceViewScope);
        }

        public void RefreshTile(Vector2Int position, Petal newPetal)
        {
            using var pieceViewScope = new PieceViewScope(pieceViewManager, nameof(RefreshTile));
            pieceViewScope.TryAcquire(position);
            PetalPresentation.RefreshTile(pieceViewManager, position, newPetal, boardLayout, pieceViewScope);
        }

        public UniTask PlayBooster(BoosterUseResult boosterUseResult) => boosterPresenter.Play(boosterUseResult);
        public void ShowBoosterTargets(BoosterType boosterType, IReadOnlyList<Vector2Int> targetPositions) => boosterPresenter.ShowBoosterTargets(boosterType, targetPositions);
        public void HideBoosterTargets() => boosterPresenter.HideBoosterTargets();
        public void SetBoosterTargetSelected(Vector2Int position, bool isSelected) => boosterPresenter.SetBoosterTargetSelected(position, isSelected);
    }
}
