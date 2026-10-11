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
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout boardLayout;
        private readonly SkillAnimationLibrary skillAnimationLibrary;
        private readonly BoosterPresenter boosterPresenter;

        public BoardVisual(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, SkillAnimationLibrary skillAnimationLibrary, BoosterPresenter boosterPresenter, BoardAudioManager boardAudioManager, BoardLayout boardLayout)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardAudioManager = boardAudioManager;
            this.boardLayout = boardLayout;
            this.skillAnimationLibrary = skillAnimationLibrary;
            this.boosterPresenter = boosterPresenter;
            boosterPresenter.Init(petalViewManager, tileViewManager, boardVFXManager, this, boardLayout);
        }

        public async UniTask PlayResolution(BoardResolution boardResolution, CancellationToken cancellationToken)
        {
            using var resolutionPlayback = new ResolutionPlayback(boardResolution, petalViewManager, tileViewManager, boardAudioManager, skillAnimationLibrary, boardLayout, cancellationToken);
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
            using var petalViewScope = new PetalViewScope(petalViewManager, nameof(PlaySwap));
            petalViewScope.AcquireRequired(fromPosition);
            petalViewScope.AcquireRequired(toPosition);
            await petalViewManager.OnSwap(fromPosition, toPosition, boardLayout.TileSize, petalViewScope);
        }

        public async UniTask PlayGravity(List<(Vector2Int fromPosition, Vector2Int toPosition)> gravityMoves)
        {
            using var petalViewScope = new PetalViewScope(petalViewManager, nameof(PlayGravity));
            foreach (var (fromPosition, _) in gravityMoves) petalViewScope.AcquireRequired(fromPosition);
            await petalViewManager.OnGravityApplied(gravityMoves, boardLayout, petalViewScope);
            if (gravityMoves.Count > 0) boardAudioManager.PlayPetalLanding();
        }

        public async UniTask PlayFill(IReadOnlyList<(Vector2Int position, Petal newPetal)> filledPetals)
        {
            using var petalViewScope = new PetalViewScope(petalViewManager, nameof(PlayFill));
            if (filledPetals.Count > 0) boardAudioManager.PlayPetalSpawning();
            await petalViewManager.OnFilled(filledPetals, boardLayout, petalViewScope);
        }

        public async UniTask PlayShuffle(IReadOnlyList<(Vector2Int position, Petal newPetal)> shuffledPetals)
        {
            using var petalViewScope = new PetalViewScope(petalViewManager, nameof(PlayShuffle));
            foreach (var (position, _) in shuffledPetals) petalViewScope.AcquireRequired(position);
            if (shuffledPetals.Count > 0) boardAudioManager.PlayBoardShuffle();
            await petalViewManager.OnShuffled(shuffledPetals, boardLayout, petalViewScope);
        }

        public void RefreshTile(Vector2Int position, Petal newPetal)
        {
            using var petalViewScope = new PetalViewScope(petalViewManager, nameof(RefreshTile));
            petalViewScope.TryAcquire(position);
            petalViewManager.RefreshTile(position, newPetal, boardLayout, petalViewScope);
        }

        public UniTask PlayBooster(BoosterUseResult boosterUseResult) => boosterPresenter.Play(boosterUseResult);
        public void ShowBoosterTargets(BoosterType boosterType, IReadOnlyList<Vector2Int> targetPositions) => boosterPresenter.ShowBoosterTargets(boosterType, targetPositions);
        public void HideBoosterTargets() => boosterPresenter.HideBoosterTargets();
        public void SetBoosterTargetSelected(Vector2Int position, bool isSelected) => boosterPresenter.SetBoosterTargetSelected(position, isSelected);
    }
}
