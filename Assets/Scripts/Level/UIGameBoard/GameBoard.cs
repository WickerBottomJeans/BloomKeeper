using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using Petals;
using Skills;
using UnityEditor;
using UnityEngine;
using Utility;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GameBoard : MonoBehaviour
{
    //TODO: maybe make the about to active petal to like blink blink or sth
    [SerializeField] private float paddingX = 0.05f;
    [SerializeField] private float paddingY = 0.05f;
    [SerializeField] private PetalViewManager petalViewManager;
    [SerializeField] private TileViewManager tileViewManager;
    [SerializeField] private BoardVFXManager boardVFXManager;
    [SerializeField] private SkillRepresentationOrchestrator skillRepresentationOrchestrator;

    [SerializeField] private BoardInputHandler boardInputHandler;

    //TODO: make it load dynamically from json 
    [SerializeField] private Texture2D boardTexture;
    private Camera cam;
    private BoardLayout layout;
    private MeshFilter meshFilter;
    private Tile[,] grid;
    
    /// <summary>
    /// Use for tester's petal editor
    /// </summary>
    private Vector2Int editingCell;
    private BoardState currentState = BoardState.Idle;
    private Vector2Int swapOrigin;
    private Vector2Int swapTarget;
    private List<MatchGroup> pendingMatches;
    private List<(Vector2Int from, Vector2Int to)> pendingMoves;
    private List<SkillActivation> pendingSkillActivations = new List<SkillActivation>();

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

    public event Action<List<PetalType>> OnPetalsCleared;

    public void Init(Tile[,] grid)
    {
        cam = Camera.main;
        this.grid = grid;
        meshFilter = GetComponent<MeshFilter>();

        layout = BoardLayoutCalculator.Calculate(
            grid.GetLength(0), grid.GetLength(1), cam, paddingX, paddingY);

        meshFilter.mesh = BoardMeshBuilder.BuildFillMesh(grid, layout, boardTexture.width / (float)boardTexture.height);

        petalViewManager.Init(grid, layout);
        tileViewManager.Init(grid, layout);
        boardVFXManager.Init(layout);
        skillRepresentationOrchestrator.Init(
            petalViewManager,
            boardVFXManager,
            layout);

        boardInputHandler.Init(layout, cam);

        boardInputHandler.OnSwapRequested += HandleSwap;
        boardInputHandler.OnEditRequested += HandleEditRequested;
        UIManager.Instance.OnPetalEditConfirmed += HandlePetalEditConfirmed;

    }

    private void HandleSwap(Vector2Int cellA, Vector2Int cellB)
    {
        if (currentState != BoardState.Idle) return;
        if (!PetalSwapper.Validate(cellA, cellB, grid)) return;

        swapOrigin = cellA;
        swapTarget = cellB;
        TransitionTo(BoardState.Swapping);
    }
    
    private void HandleEditRequested(Vector2Int cell)
    {
        if (currentState != BoardState.Idle) return;

        editingCell = cell;

        Vector2 worldPos = layout.GetCellWorldPos(cell.x, cell.y);
        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);

        UIManager.Instance.ShowPetalEditorPopup(screenPos);
    }

    private void HandlePetalEditConfirmed(PetalType petalType, SpecialSkillType skillType)
    {
        Petal petal = new Petal(petalType, skillType);
        grid[editingCell.x, editingCell.y].Petal = petal;
        petalViewManager.RefreshCell(editingCell, petal, layout);
    }

    private void TransitionTo(BoardState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case BoardState.Swapping: EnterSwapping(); break;
            case BoardState.SwappingBack: EnterSwappingBack(); break;
            case BoardState.Resolving: EnterResolving(); break;
            case BoardState.ActivatingSkills: EnterActivatingSkills(); break;
            case BoardState.Gravity: EnterGravity(); break;
            case BoardState.Filling: EnterFilling(); break;
            case BoardState.Cascade: EnterCascade(); break;
            case BoardState.Idle: EnterIdle(); break;
            case BoardState.Shuffling: EnterShuffling(); break;

        }
    }
    
    private async UniTask EnterSwapping()
    {
        pendingSkillActivations.Clear();
        pendingMatches = new List<MatchGroup>();

        PetalSwapper.ExecuteSwapPetal(swapOrigin, swapTarget, grid);
        await petalViewManager.OnSwap(swapOrigin, swapTarget);

        pendingSkillActivations.AddRange(SkillDetector.DetectOnSwap(grid, swapOrigin, swapTarget));

        if (pendingSkillActivations.Count > 0)
        {
            pendingMatches.Add(new MatchGroup(new List<Vector2Int> { swapOrigin, swapTarget }, MatchShape.None, isFromSkillCombo: true));
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

        TransitionTo(BoardState.Resolving);
    }

    private async UniTask EnterSwappingBack()
    {
        await petalViewManager.OnSwap(swapTarget, swapOrigin);
        TransitionTo(BoardState.Idle);
    }

    private async UniTask EnterResolving()
    {
        var result = MatchResolver.Resolve(pendingMatches, grid, swapOrigin, swapTarget);
        pendingMatches.Clear();
        pendingSkillActivations.AddRange(result.SkillActivations);
        OnPetalsCleared?.Invoke(result.ClearedPetalTypes);
        await UniTask.WhenAll(
            tileViewManager.OnMatchResolved(result, grid),
            petalViewManager.OnMatchResolved(result, layout)
        );
            TransitionTo(BoardState.ActivatingSkills);
    }

    private async UniTask EnterActivatingSkills()
    {
        if (pendingSkillActivations.Count == 0)
        {
            TransitionTo(BoardState.Gravity);
            return;
        }

        pendingMatches = new List<MatchGroup>();
        var skillResults = new List<SkillUseResult>();

        foreach (SkillActivation activation in pendingSkillActivations)
        {
            SkillUseResult result = SkillManager.UseSkill(grid, activation);
            skillResults.Add(result);
            pendingMatches.Add(result.MatchGroup);
        }

        pendingSkillActivations.Clear();
        await skillRepresentationOrchestrator.Play(skillResults);
        TransitionTo(BoardState.Resolving);
    }

    private async UniTask EnterGravity()
    {
        pendingMoves = GravityController.Apply(grid);
        await petalViewManager.OnGravityApplied(pendingMoves, layout);
        TransitionTo(BoardState.Filling);
    }

    private async UniTask EnterFilling()
    {
        List<Vector2Int> filled = PetalFiller.Fill(grid);
        await petalViewManager.OnFilled(filled, layout, grid);
        TransitionTo(BoardState.Cascade);
    }

    private void EnterCascade()
    {
        List<MatchGroup> cascadeMatches = MatchDetector.Detect(grid);
        if (cascadeMatches.Count == 0)
        {
            TransitionTo(BoardState.Idle);
            return;
        }

        pendingMatches = cascadeMatches;
        TransitionTo(BoardState.Resolving);
    }

    private void EnterIdle()
    {
        if (!DeadlockDetector.HasValidMove(grid))
            TransitionTo(BoardState.Shuffling);
    }

    private async UniTask EnterShuffling()
    {
        List<Vector2Int> shuffled = BoardShuffler.Shuffle(grid);
        await petalViewManager.OnShuffled(shuffled, layout, grid);
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

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Tile tile = grid[x, y];
                Vector2 center = layout.GetCellWorldPos(x, y);

                Gizmos.color = tile switch
                {
                    InactiveTile => new Color(0.55f, 0.1f, 0.1f, 0.8f),
                    WebTile => new Color(0.6f, 0.6f, 0.6f, 0.8f),
                    NormalTile => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                    _ => Color.white
                };

                Gizmos.DrawWireCube(center, Vector3.one * layout.CellSize * 0.95f);
            }
        }

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Tile tile = grid[x, y];
                if (tile?.Petal == null) continue;
                Vector2 center = layout.GetCellWorldPos(x, y);
                Handles.Label(center, tile.Petal.PetalType.ToString()[0].ToString());
                int i = 1;
            }
        }
    }
}
