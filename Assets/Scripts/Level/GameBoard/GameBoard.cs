using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Boosters;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using Petals;
using Skills;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Utility;

[RequireComponent(typeof(BoosterRepresentationOrchestrator))]
public class GameBoard : MonoBehaviour
{
    //TODO: maybe make the about to active petal to like blink blink or sth
    [SerializeField] private float paddingX = 0.05f;
    [SerializeField] private float paddingY = 0.05f;
    [SerializeField] private PetalViewManager petalViewManager;
    [SerializeField] private TileViewManager tileViewManager;
    [SerializeField] private BoardVFXManager boardVFXManager;
    [SerializeField] private SkillRepresentationOrchestrator skillRepresentationOrchestrator;
    [SerializeField] private BoardAudioManager boardAudioManager;

    [SerializeField] private BoardInputHandler boardInputHandler;

    private Camera cam;
    private BoardLayout layout;
    private Tile[,] grid;

    /// <summary>
    /// Use for tester's petal editor
    /// </summary>
    private Vector2Int editingTile;

    private BoardState currentState = BoardState.Idle;
    private Vector2Int swapOrigin;
    private Vector2Int swapTarget;
    private List<MatchGroup> pendingMatches;
    private List<(Vector2Int from, Vector2Int to)> pendingMoves;
    private List<SkillActivation> pendingSkillActivations = new List<SkillActivation>();
    private IReadOnlyList<Vector2Int> pendingPreferredSkillSpawnPositions = Array.Empty<Vector2Int>();
    private BoardPresentationCoordinator boardPresentationCoordinator;
    private BoosterRepresentationOrchestrator boosterRepresentationOrchestrator;
    private BoosterUseResult pendingBoosterUseResult;
    private Func<IReadOnlyList<TileState>, IReadOnlyList<ObjectiveTileTargetGroup>> resolveObjectiveTargets;
    private bool isResolvingPlayerInitiatedAction;
    private int currentCascadeDepth;
    private IBoosterChooser activeBoosterChooser;
    private BoosterType? activeBoosterType;

    private enum BoardState
    {
        Idle,
        Swapping,
        SwappingBack,
        Resolving,
        Gravity,
        Filling,
        Cascade,
        Shuffling,
        BoosterTargeting,
        BoosterAuthorization
    }

    public event Action<IGameplayEvent> OnGameplayEvent;
    public event Action<bool> IdleStateChanged;

    public bool IsIdle => currentState == BoardState.Idle;

    public void Init(Tile[,] grid, Rect playAreaScreenRect,
        Func<IReadOnlyList<TileState>, IReadOnlyList<ObjectiveTileTargetGroup>> resolveObjectiveTargets)
    {
        cam = Camera.main;
        this.grid = grid;
        this.resolveObjectiveTargets = resolveObjectiveTargets;

        layout = BoardLayoutCalculator.Calculate(grid.GetLength(0), grid.GetLength(1), cam, paddingX, paddingY,
            playAreaScreenRect);

        petalViewManager.Init(grid, layout);
        tileViewManager.Init(grid, layout);
        boardVFXManager.Init(layout, cam);
        skillRepresentationOrchestrator.Init(petalViewManager, tileViewManager, boardVFXManager, boardAudioManager,
            layout);
        boosterRepresentationOrchestrator = GetComponent<BoosterRepresentationOrchestrator>();
        boardPresentationCoordinator = new BoardPresentationCoordinator(petalViewManager, tileViewManager, boardVFXManager, skillRepresentationOrchestrator, boosterRepresentationOrchestrator, boardAudioManager, layout, grid);

        boardInputHandler.Init(layout, cam);

        boardInputHandler.OnSwapRequested += HandleSwap;
        boardInputHandler.OnEditRequested += HandleEditRequested;
        UIManager.Instance.OnPetalEditConfirmed += HandlePetalEditConfirmed;

    }

    public async UniTask<BoosterTargetSelectionResult> TrySelectBoosterTargets(BoosterType boosterType)
    {
        if (currentState != BoardState.Idle) return BoosterTargetSelectionResult.Unavailable;
        if (activeBoosterChooser != null) throw new InvalidOperationException("A booster is already targeting the board.");

        activeBoosterType = boosterType;
        TransitionTo(BoardState.BoosterTargeting);
        return await RunBoosterTargetSelection();
    }

    public void CancelBoosterTargeting()
    {
        if (activeBoosterChooser == null) return;

        activeBoosterChooser.Cancel();
    }

    public void ExecuteApprovedBooster(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
    {
        if (currentState != BoardState.BoosterAuthorization) throw new InvalidOperationException("A booster can only execute while waiting for authorization.");
        if (!activeBoosterType.HasValue || activeBoosterType.Value != boosterType) throw new InvalidOperationException("Approved booster does not match the active booster use.");
        if (targets == null) throw new ArgumentNullException(nameof(targets));

        try
        {
            pendingBoosterUseResult = BoosterManager.Execute(boosterType, grid, targets);
            activeBoosterType = null;
            LoadResolutionInput(pendingBoosterUseResult.ResolutionInput);
            isResolvingPlayerInitiatedAction = true;
            currentCascadeDepth = 0;
            TransitionTo(BoardState.Resolving);
        }
        catch
        {
            activeBoosterType = null;
            pendingBoosterUseResult = null;
            TransitionTo(BoardState.Idle);
            throw;
        }
    }

    public void RejectPendingBoosterUse()
    {
        if (currentState != BoardState.BoosterAuthorization) throw new InvalidOperationException("A booster rejection requires pending authorization.");
        if (!activeBoosterType.HasValue) throw new InvalidOperationException("A booster rejection requires an active booster type.");

        activeBoosterType = null;
        TransitionTo(BoardState.Idle);
    }

    private void HandleSwap(Vector2Int tileA, Vector2Int tileB)
    {
        if (currentState != BoardState.Idle) return;
        if (!PetalSwapper.Validate(tileA, tileB, grid)) return;

        swapOrigin = tileA;
        swapTarget = tileB;
        TransitionTo(BoardState.Swapping);
    }

    private void HandleEditRequested(Vector2Int tile)
    {
        if (currentState != BoardState.Idle) return;
        Tile selectedTile = grid[tile.x, tile.y];
        if (selectedTile == null || (!selectedTile.CanReceiveNewPetal() && !selectedTile.CanSwapPetal())) return;

        editingTile = tile;

        Vector2 worldPos = layout.GetTileWorldPos(tile.x, tile.y);
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);

        UIManager.Instance.ShowPetalEditorPopup(screenPos);
    }

    private void HandlePetalEditConfirmed(PetalType petalType, SpecialSkillType skillType)
    {
        if (grid[editingTile.x, editingTile.y] == null) return;

        Petal petal = new Petal(petalType, skillType);
        grid[editingTile.x, editingTile.y].SetPetal(petal);
        boardPresentationCoordinator.RefreshTile(editingTile, petal);
    }

    private void TransitionTo(BoardState newState)
    {
        bool wasIdle = currentState == BoardState.Idle;
        currentState = newState;
        if (wasIdle && newState != BoardState.Idle)
            IdleStateChanged?.Invoke(false);

        switch (newState)
        {
            case BoardState.Swapping: EnterSwapping().Forget(); break;
            case BoardState.SwappingBack: EnterSwappingBack().Forget(); break;
            case BoardState.Resolving: EnterResolving().Forget(); break;
            case BoardState.Gravity: EnterGravity().Forget(); break;
            case BoardState.Filling: EnterFilling().Forget(); break;
            case BoardState.Cascade: EnterCascade(); break;
            case BoardState.Idle: EnterIdle(); break;
            case BoardState.Shuffling: EnterShuffling().Forget(); break;
        }
    }

    private async UniTask<BoosterTargetSelectionResult> RunBoosterTargetSelection()
    {
        IBoosterChooser chooser = null;
        bool boosterTargetsShown = false;
        try
        {
            if (!activeBoosterType.HasValue) throw new InvalidOperationException("Booster targeting requires an active booster type.");

            chooser = BoosterManager.CreateChooser(activeBoosterType.Value);
            activeBoosterChooser = chooser;
            chooser.TargetSelectionChanged += HandleBoosterTargetSelectionChanged;
            IReadOnlyList<Vector2Int> boosterTargetCandidates = activeBoosterChooser.GetBoosterTargetCandidates(grid);
            boardPresentationCoordinator.ShowBoosterTargets(activeBoosterType.Value, boosterTargetCandidates);
            boosterTargetsShown = true;

            BoosterTargetSelectionResult result = await activeBoosterChooser.Choose(grid, boardInputHandler);
            activeBoosterChooser = null;

            if (result.IsCanceled)
            {
                activeBoosterType = null;
                TransitionTo(BoardState.Idle);
                return result;
            }

            TransitionTo(BoardState.BoosterAuthorization);
            return result;
        }
        catch
        {
            activeBoosterChooser = null;
            activeBoosterType = null;
            pendingBoosterUseResult = null;
            TransitionTo(BoardState.Idle);
            throw;
        }
        finally
        {
            if (chooser != null)
                chooser.TargetSelectionChanged -= HandleBoosterTargetSelectionChanged;
            if (boosterTargetsShown)
                boardPresentationCoordinator.HideBoosterTargets();
        }
    }

    private void HandleBoosterTargetSelectionChanged(Vector2Int position, bool isSelected)
    {
        boardPresentationCoordinator.SetBoosterTargetSelected(position, isSelected);
    }

    private async UniTask EnterSwapping()
    {
        pendingSkillActivations.Clear();
        pendingMatches = new List<MatchGroup>();
        pendingPreferredSkillSpawnPositions = Array.Empty<Vector2Int>();
        currentCascadeDepth = 0;

        BoardResolutionInput resolutionInput = BoardSwapOperation.Execute(grid, swapOrigin, swapTarget);
        await boardPresentationCoordinator.PlaySwap(swapOrigin, swapTarget);

        if (!resolutionInput.RequiresResolution)
        {
            PetalSwapper.ExecuteSwapPetal(swapOrigin, swapTarget, grid);
            TransitionTo(BoardState.SwappingBack);
            return;
        }

        LoadResolutionInput(resolutionInput);
        isResolvingPlayerInitiatedAction = true;
        OnGameplayEvent?.Invoke(new PlayerMoveCommittedEvent());
        TransitionTo(BoardState.Resolving);
    }

    private async UniTask EnterSwappingBack()
    {
        await boardPresentationCoordinator.PlayInvalidSwapBack(swapTarget, swapOrigin);
        TransitionTo(BoardState.Idle);
    }

    private async UniTask EnterResolving()
    {
        if (pendingBoosterUseResult != null)
        {
            BoosterUseResult boosterUseResult = pendingBoosterUseResult;
            await boardPresentationCoordinator.PlayBooster(boosterUseResult);
            pendingBoosterUseResult = null;

            if (!boosterUseResult.ResolutionInput.RequiresResolution)
            {
                pendingPreferredSkillSpawnPositions = Array.Empty<Vector2Int>();
                TransitionTo(BoardState.Idle);
                return;
            }
        }

        TurnResolution turnResolution = CalculateTurnResolution();
        await PlayTurnResolution(turnResolution);
        TransitionTo(BoardState.Gravity);
    }

    private TurnResolution CalculateTurnResolution()
    {
        MatchResolveResult initialMatch = null;
        var skillWaves = new List<SkillResolutionWave>();

        if (pendingSkillActivations.Count > 0)
        {
            IReadOnlyList<ObjectiveTileTargetGroup> objectiveTargetGroups = resolveObjectiveTargets(BoardSnapshotBuilder.Capture(grid));
            List<SkillUseResult> openingSkillResults = SkillManager.UseSkills(grid, pendingSkillActivations, objectiveTargetGroups);
            pendingSkillActivations.Clear();
            MatchResolveResult openingResolution = MatchResolver.Resolve(openingSkillResults, grid, pendingPreferredSkillSpawnPositions);
            skillWaves.Add(new SkillResolutionWave(openingResolution, openingSkillResults));
            AddSkillActivations(openingResolution);
        }
        else
        {
            initialMatch = MatchResolver.Resolve(pendingMatches, grid, pendingPreferredSkillSpawnPositions);
            pendingMatches.Clear();
            AddSkillActivations(initialMatch);
        }

        pendingPreferredSkillSpawnPositions = Array.Empty<Vector2Int>();

        while (pendingSkillActivations.Count > 0)
        {
            IReadOnlyList<ObjectiveTileTargetGroup> objectiveTargetGroups = resolveObjectiveTargets(BoardSnapshotBuilder.Capture(grid));
            List<SkillUseResult> skillResults = SkillManager.UseSkills(grid, pendingSkillActivations, objectiveTargetGroups);
            pendingSkillActivations.Clear();

            foreach (SkillUseResult skillResult in skillResults)
                pendingMatches.Add(skillResult.MatchGroup);

            MatchResolveResult resolution = MatchResolver.Resolve(pendingMatches, grid, Array.Empty<Vector2Int>());
            pendingMatches.Clear();
            skillWaves.Add(new SkillResolutionWave(resolution, skillResults));
            AddSkillActivations(resolution);
        }

        return new TurnResolution(initialMatch, skillWaves);
    }

    private void AddSkillActivations(MatchResolveResult resolution)
    {
        foreach (MatchGroupResolveResult groupResult in resolution.GroupResults)
            pendingSkillActivations.AddRange(groupResult.SkillActivations);
    }

    private void LoadResolutionInput(BoardResolutionInput resolutionInput)
    {
        pendingMatches = new List<MatchGroup>(resolutionInput.MatchGroups);
        pendingSkillActivations.Clear();
        pendingSkillActivations.AddRange(resolutionInput.SkillActivations);
        pendingPreferredSkillSpawnPositions = resolutionInput.PreferredSkillSpawnPositions;
    }

    private async UniTask PlayTurnResolution(TurnResolution turnResolution)
    {
        int nextSkillWaveIndex;
        if (turnResolution.InitialMatch == null)
        {
            if (turnResolution.SkillWaves.Count == 0)
                throw new InvalidOperationException("Turn resolution requires an initial match or at least one skill wave.");

            SkillResolutionWave openingSkillWave = turnResolution.SkillWaves[0];
            await boardPresentationCoordinator.PlaySkillWave(openingSkillWave);
            OnGameplayEvent?.Invoke(new BoardResolutionStepCompletedEvent(openingSkillWave.Resolution, currentCascadeDepth, isResolvingPlayerInitiatedAction));
            nextSkillWaveIndex = 1;
        }
        else
        {
            if (turnResolution.SkillWaves.Count == 0)
            {
                await boardPresentationCoordinator.PlayInitialMatch(turnResolution.InitialMatch);
                OnGameplayEvent?.Invoke(new BoardResolutionStepCompletedEvent(turnResolution.InitialMatch, currentCascadeDepth, isResolvingPlayerInitiatedAction));
                return;
            }

            SkillResolutionWave firstSkillWave = turnResolution.SkillWaves[0];
            await UniTask.WhenAll(boardPresentationCoordinator.PlayInitialMatch(turnResolution.InitialMatch), boardPresentationCoordinator.PlaySkillWave(firstSkillWave));
            OnGameplayEvent?.Invoke(new BoardResolutionStepCompletedEvent(turnResolution.InitialMatch, currentCascadeDepth, isResolvingPlayerInitiatedAction));
            OnGameplayEvent?.Invoke(new BoardResolutionStepCompletedEvent(firstSkillWave.Resolution, currentCascadeDepth, isResolvingPlayerInitiatedAction));
            nextSkillWaveIndex = 1;
        }

        for (int i = nextSkillWaveIndex; i < turnResolution.SkillWaves.Count; i++)
        {
            SkillResolutionWave skillWave = turnResolution.SkillWaves[i];
            await boardPresentationCoordinator.PlaySkillWave(skillWave);
            OnGameplayEvent?.Invoke(new BoardResolutionStepCompletedEvent(skillWave.Resolution, currentCascadeDepth,
                isResolvingPlayerInitiatedAction));
        }
    }

    private async UniTask EnterGravity()
    {
        pendingMoves = GravityController.Apply(grid);
        await boardPresentationCoordinator.PlayGravity(pendingMoves);
        TransitionTo(BoardState.Filling);
    }

    private async UniTask EnterFilling()
    {
        List<Vector2Int> filled = PetalFiller.Fill(grid);
        await boardPresentationCoordinator.PlayFill(filled);
        TransitionTo(BoardState.Cascade);
    }

    private void EnterCascade()
    {
        List<MatchGroup> cascadeMatches = MatchDetector.Detect(grid);
        if (cascadeMatches.Count == 0)
        {
            isResolvingPlayerInitiatedAction = false;
            currentCascadeDepth = 0;
            TransitionTo(BoardState.Idle);
            return;
        }

        currentCascadeDepth++;
        pendingMatches = cascadeMatches;
        pendingPreferredSkillSpawnPositions = Array.Empty<Vector2Int>();
        TransitionTo(BoardState.Resolving);
    }

    private void EnterIdle()
    {
        if (!DeadlockDetector.HasValidMove(grid))
        {
            TransitionTo(BoardState.Shuffling);
            return;
        }

        IdleStateChanged?.Invoke(true);
    }

    private async UniTask EnterShuffling()
    {
        currentCascadeDepth = 0;
        List<Vector2Int> shuffled = BoardShuffler.Shuffle(grid);
        await boardPresentationCoordinator.PlayShuffle(shuffled);
        TransitionTo(BoardState.Cascade);
    }

    private void OnDestroy()
    {
        if (boardInputHandler != null)
        {
            boardInputHandler.OnSwapRequested -= HandleSwap;
            boardInputHandler.OnEditRequested -= HandleEditRequested;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnPetalEditConfirmed -= HandlePetalEditConfirmed;
        }
    }
}
