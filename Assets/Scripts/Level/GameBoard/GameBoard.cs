using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private List<SkillUseResult> pendingSkillResults = new List<SkillUseResult>();
    private BoardPresentationCoordinator boardPresentationCoordinator;
    private Func<IReadOnlyList<TileState>, IReadOnlyList<ObjectiveTileTargetGroup>> resolveObjectiveTargets;
    private bool isResolvingPlayerMove;
    private int currentCascadeDepth;

    private enum BoardState
    {
        Idle,
        Swapping,
        SwappingBack,
        Resolving,
        ActivatingSkills,
        Gravity,
        Filling,
        Cascade,
        Shuffling
    }

    public event Action<IGameplayEvent> OnGameplayEvent;
    public event Action OnBoardSettled;

    public void Init(Tile[,] grid, Rect playAreaScreenRect, Func<IReadOnlyList<TileState>, IReadOnlyList<ObjectiveTileTargetGroup>> resolveObjectiveTargets)
    {
        cam = Camera.main;
        this.grid = grid;
        this.resolveObjectiveTargets = resolveObjectiveTargets;

        layout = BoardLayoutCalculator.Calculate(grid.GetLength(0), grid.GetLength(1), cam, paddingX, paddingY, playAreaScreenRect);

        petalViewManager.Init(grid, layout);
        tileViewManager.Init(grid, layout);
        boardVFXManager.Init(layout);
        skillRepresentationOrchestrator.Init(petalViewManager, tileViewManager, boardVFXManager, boardAudioManager, layout);
        boardPresentationCoordinator = new BoardPresentationCoordinator(petalViewManager, tileViewManager, skillRepresentationOrchestrator, boardAudioManager, layout, grid);

        boardInputHandler.Init(layout, cam);

        boardInputHandler.OnSwapRequested += HandleSwap;
        boardInputHandler.OnEditRequested += HandleEditRequested;
        UIManager.Instance.OnPetalEditConfirmed += HandlePetalEditConfirmed;

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
        if (grid[tile.x, tile.y] == null) return;

        editingTile = tile;

        Vector2 worldPos = layout.GetTileWorldPos(tile.x, tile.y);
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);

        UIManager.Instance.ShowPetalEditorPopup(screenPos);
    }

    private void HandlePetalEditConfirmed(PetalType petalType, SpecialSkillType skillType)
    {
        if (grid[editingTile.x, editingTile.y] == null) return;

        Petal petal = new Petal(petalType, skillType);
        grid[editingTile.x, editingTile.y].Petal = petal;
        boardPresentationCoordinator.RefreshTile(editingTile, petal);
    }

    private void TransitionTo(BoardState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case BoardState.Swapping: EnterSwapping().Forget(); break;
            case BoardState.SwappingBack: EnterSwappingBack().Forget(); break;
            case BoardState.Resolving: EnterResolving().Forget(); break;
            case BoardState.ActivatingSkills: EnterActivatingSkills(); break;
            case BoardState.Gravity: EnterGravity().Forget(); break;
            case BoardState.Filling: EnterFilling().Forget(); break;
            case BoardState.Cascade: EnterCascade(); break;
            case BoardState.Idle: EnterIdle(); break;
            case BoardState.Shuffling: EnterShuffling().Forget(); break;

        }
    }
    
    private async UniTask EnterSwapping()
    {
        pendingSkillActivations.Clear();
        pendingSkillResults.Clear();
        pendingMatches = new List<MatchGroup>();
        currentCascadeDepth = 0;

        PetalSwapper.ExecuteSwapPetal(swapOrigin, swapTarget, grid);
        await boardPresentationCoordinator.PlaySwap(swapOrigin, swapTarget);

        pendingSkillActivations.AddRange(SkillDetector.DetectOnSwap(grid, swapOrigin, swapTarget));

        if (pendingSkillActivations.Count > 0)
        {
            pendingMatches.Add(new MatchGroup(new List<Vector2Int> { swapOrigin, swapTarget }, MatchShape.None, isFromSkillCombo: true));
            isResolvingPlayerMove = true;
            OnGameplayEvent?.Invoke(new PlayerMoveCommittedEvent());
            TransitionTo(BoardState.Resolving);
            return;
        }

        pendingMatches = MatchDetector.Detect(grid);

        if (pendingMatches.Count == 0)
        {
            PetalSwapper.ExecuteSwapPetal(swapOrigin, swapTarget, grid);
            TransitionTo(BoardState.SwappingBack);
            return;
        }

        isResolvingPlayerMove = true;
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
        MatchResolveResult result = MatchResolver.Resolve(pendingMatches, grid, swapOrigin, swapTarget);
        pendingMatches.Clear();
        foreach (MatchGroupResolveResult groupResult in result.GroupResults)
            pendingSkillActivations.AddRange(groupResult.SkillActivations);
        await boardPresentationCoordinator.PlayMatch(result, pendingSkillResults);
        OnGameplayEvent?.Invoke(new BoardResolvedEvent(result, currentCascadeDepth, isResolvingPlayerMove));
        pendingSkillResults.Clear();
        TransitionTo(BoardState.ActivatingSkills);
    }

    private void EnterActivatingSkills()
    {
        if (pendingSkillActivations.Count == 0)
        {
            TransitionTo(BoardState.Gravity);
            return;
        }

        pendingMatches = new List<MatchGroup>();
        pendingSkillResults.Clear();
        IReadOnlyList<ObjectiveTileTargetGroup> objectiveTargetGroups = resolveObjectiveTargets(BoardSnapshotBuilder.Capture(grid));
        List<SkillUseResult> skillResults = SkillManager.UseSkills(grid, pendingSkillActivations, objectiveTargetGroups);
        pendingSkillResults.AddRange(skillResults);

        foreach (SkillUseResult result in skillResults)
            pendingMatches.Add(result.MatchGroup);

        pendingSkillActivations.Clear();
        TransitionTo(BoardState.Resolving);
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
            isResolvingPlayerMove = false;
            currentCascadeDepth = 0;
            TransitionTo(BoardState.Idle);
            return;
        }

        currentCascadeDepth++;
        pendingMatches = cascadeMatches;
        TransitionTo(BoardState.Resolving);
    }

    private void EnterIdle()
    {
        if (!DeadlockDetector.HasValidMove(grid))
        {
            TransitionTo(BoardState.Shuffling);
            return;
        }

        OnBoardSettled?.Invoke();
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
            boardInputHandler.OnEditRequested -= HandleEditRequested;

        if (UIManager.Instance != null)
            UIManager.Instance.OnPetalEditConfirmed -= HandlePetalEditConfirmed;
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (grid == null) return;
        if (layout == null) return;

#if UNITY_EDITOR
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Tile tile = grid[x, y];
                if (tile == null) continue;
                Vector2 center = layout.GetTileWorldPos(x, y);

                Gizmos.color = tile switch
                {
                    InactiveTile => new Color(0.55f, 0.1f, 0.1f, 0.8f),
                    WebTile => new Color(0.6f, 0.6f, 0.6f, 0.8f),
                    NormalTile => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                    _ => Color.white
                };

                Gizmos.DrawWireCube(center, Vector3.one * layout.TileSize * 0.95f);
            }
        }

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Tile tile = grid[x, y];
                if (tile == null || tile.Petal == null) continue;
                Vector2 center = layout.GetTileWorldPos(x, y);
                Handles.Label(center, tile.Petal.PetalType.ToString()[0].ToString());
            }
        }
#endif
    }
}
