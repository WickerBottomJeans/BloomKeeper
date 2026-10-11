using System;
using System.Collections.Generic;
using Boosters;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using Skills;
using UnityEngine;
using Utility;

/// <summary>
/// The board's orchestrator and composition root. Wires its collaborators, gates input,
/// and sequences swap/booster, resolve, presentation, gravity, refill, and shuffle.
/// Keep the turn sequence here. Delegate gameplay rules and grid mutations to BoardLogic,
/// animations to BoardVisual, booster interaction to BoardBoosterInteraction,
/// and admin editing to BoardPetalEditor. Do not implement those details in this class.
/// Board state describes availability only (Idle/Busy); booster phases belong to its interaction.
/// </summary>
[RequireComponent(typeof(BoosterPresenter))]
public class GameBoard : MonoBehaviour
{
    [SerializeField] private PetalViewManager petalViewManager;
    [SerializeField] private TileViewManager tileViewManager;
    [SerializeField] private BoardVFXManager boardVFXManager;
    [SerializeField] private SkillAnimationLibrary skillAnimationLibrary;
    [SerializeField] private BoardAudioManager boardAudioManager;
    [SerializeField] private BoardInputHandler boardInputHandler;

    private BoardLogic boardLogic;
    private BoardVisual boardVisual;

    private BoardState availabilityState = BoardState.Idle;
    private BoardBoosterInteraction boardBoosterInteraction;
    private BoardPetalEditor boardPetalEditor;
    private Func<IReadOnlyList<TileState>, IReadOnlyList<ObjectiveTileTargetGroup>> resolveObjectiveTargets;

    private enum BoardState
    {
        Idle,
        Busy
    }

    public event Action<IGameplayEvent> OnGameplayEvent;
    public event Action<bool> IdleStateChanged;

    public bool IsIdle => availabilityState == BoardState.Idle;

    public void Init(Tile[,] boardGrid, Rect playAreaScreenRect, Func<IReadOnlyList<TileState>, IReadOnlyList<ObjectiveTileTargetGroup>> resolveObjectiveTargets)
    {
        Camera camera = Camera.main;
        boardLogic = new BoardLogic(boardGrid);
        this.resolveObjectiveTargets = resolveObjectiveTargets;

        BoardLayout boardLayout = BoardLayoutCalculator.Calculate(boardGrid.GetLength(0), boardGrid.GetLength(1), camera, playAreaScreenRect);

        petalViewManager.Init(boardGrid, boardLayout);
        tileViewManager.Init(boardGrid, boardLayout);
        boardVFXManager.Init(boardLayout, camera);
        skillAnimationLibrary.Init(petalViewManager, boardVFXManager, boardAudioManager, boardLayout);
        BoosterPresenter boosterPresenter = GetComponent<BoosterPresenter>();
        boardVisual = new BoardVisual(petalViewManager, tileViewManager, boardVFXManager, skillAnimationLibrary, boosterPresenter, boardAudioManager, boardLayout);

        boardInputHandler.Init(boardLayout, camera);

        boardInputHandler.OnSwapRequested += HandleSwap;
        boardBoosterInteraction = new BoardBoosterInteraction(boardGrid, boardInputHandler, boardVisual);
        boardPetalEditor = new BoardPetalEditor(boardLogic, boardLayout, camera, boardInputHandler, boardVisual, UIManager.Instance, () => IsIdle);
    }

    public async UniTask<BoosterTargetSelectionResult> TrySelectBoosterTargets(BoosterType boosterType)
    {
        if (!IsIdle) return BoosterTargetSelectionResult.Unavailable;
        SetState(BoardState.Busy);
        try
        {
            BoosterTargetSelectionResult boosterTargetSelectionResult = await boardBoosterInteraction.Select(boosterType);
            if (boosterTargetSelectionResult.IsCanceled || boosterTargetSelectionResult.IsUnavailable) ResolveBoard().Forget();
            return boosterTargetSelectionResult;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            ResolveBoard().Forget();
            throw;
        }
    }

    public void CancelBoosterTargeting() => boardBoosterInteraction.Cancel();

    public void ExecuteApprovedBooster(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
    {
        boardBoosterInteraction.ValidateApproval(boosterType, targets);
        BoosterUseResult boosterUseResult;
        try
        {
            boosterUseResult = boardLogic.BeginBooster(boosterType, targets);
        }
        catch
        {
            boardBoosterInteraction.Complete();
            ResolveBoard().Forget();
            throw;
        }
        boardBoosterInteraction.Complete();
        SetState(BoardState.Busy);
        RunBooster(boosterUseResult).Forget();
    }

    public void RejectPendingBoosterUse()
    {
        boardBoosterInteraction.Complete();
        ResolveBoard().Forget();
    }

    private void HandleSwap(Vector2Int tileAPosition, Vector2Int tileBPosition)
    {
        if (availabilityState != BoardState.Idle) return;
        RunSwap(tileAPosition, tileBPosition).Forget();
    }

    private void SetState(BoardState newState)
    {
        bool wasIdle = availabilityState == BoardState.Idle;
        availabilityState = newState;
        if (wasIdle != IsIdle)
            IdleStateChanged?.Invoke(IsIdle);
    }

    private async UniTask RunSwap(Vector2Int swapOrigin, Vector2Int swapTarget)
    {
        if (!boardLogic.TryBeginSwap(swapOrigin, swapTarget, out BoardResolutionInput resolutionInput)) return;
        SetState(BoardState.Busy);
        await boardVisual.PlaySwap(swapOrigin, swapTarget);

        if (!resolutionInput.RequiresResolution)
        {
            await boardVisual.PlayInvalidSwapBack(swapTarget, swapOrigin);
        }
        else
            OnGameplayEvent?.Invoke(new PlayerMoveCommittedEvent());

        await ResolveBoard(resolutionInput, resolutionInput.RequiresResolution);
    }

    private async UniTask RunBooster(BoosterUseResult boosterUseResult)
    {
        await boardVisual.PlayBooster(boosterUseResult);
        await ResolveBoard(boosterUseResult.ResolutionInput, true);
    }

    private async UniTask ResolveBoard(BoardResolutionInput resolutionInput = null, bool isPlayerInitiated = false)
    {
        SetState(BoardState.Busy);
        int cascadeDepth = 0;

        while (true)
        {
            if (resolutionInput != null && resolutionInput.RequiresResolution)
            {
                BoardResolution boardResolution = boardLogic.Resolve(resolutionInput, resolveObjectiveTargets);
                await boardVisual.PlayResolution(boardResolution, this.GetCancellationTokenOnDestroy());
                OnGameplayEvent?.Invoke(new BoardResolutionStepCompletedEvent(boardResolution, cascadeDepth, isPlayerInitiated));

                List<(Vector2Int from, Vector2Int to)> gravityMoves = boardLogic.ApplyGravity();
                await boardVisual.PlayGravity(gravityMoves);

                var filledPetals = boardLogic.Fill();
                await boardVisual.PlayFill(filledPetals);

                List<MatchGroup> cascadeMatches = boardLogic.DetectMatches();
                if (cascadeMatches.Count > 0)
                {
                    cascadeDepth++;
                    resolutionInput = new BoardResolutionInput(cascadeMatches, Array.Empty<SkillActivation>());
                    continue;
                }

                isPlayerInitiated = false;
            }

            if (boardLogic.HasValidMove())
            {
                SetState(BoardState.Idle);
                return;
            }

            var shuffledPetals = boardLogic.Reroll();
            await boardVisual.PlayShuffle(shuffledPetals);

            List<MatchGroup> shuffleMatches = boardLogic.DetectMatches();
            resolutionInput = new BoardResolutionInput(shuffleMatches, Array.Empty<SkillActivation>());
            cascadeDepth = shuffleMatches.Count > 0 ? 1 : 0;
            if (shuffleMatches.Count == 0)
                isPlayerInitiated = false;
        }
    }

    private void OnDestroy()
    {
        if (boardInputHandler != null) boardInputHandler.OnSwapRequested -= HandleSwap;
        boardPetalEditor?.Dispose();
        boardBoosterInteraction?.Dispose();
    }
}
