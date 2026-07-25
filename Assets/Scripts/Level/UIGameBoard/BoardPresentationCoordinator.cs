using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using Skills;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class BoardPresentationCoordinator
    {
        private readonly PetalViewManager petalViewManager;
        private readonly MatchPresentationCoordinator matchPresentationCoordinator;
        private readonly SkillRepresentationOrchestrator skillRepresentationOrchestrator;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout layout;
        private readonly BoardCell[,] grid;

        public BoardPresentationCoordinator(PetalViewManager petalViewManager, TileViewManager tileViewManager, SkillRepresentationOrchestrator skillRepresentationOrchestrator, BoardAudioManager boardAudioManager, BoardLayout layout, BoardCell[,] grid)
        {
            this.petalViewManager = petalViewManager;
            this.skillRepresentationOrchestrator = skillRepresentationOrchestrator;
            this.boardAudioManager = boardAudioManager;
            this.layout = layout;
            this.grid = grid;
            matchPresentationCoordinator = new MatchPresentationCoordinator(petalViewManager, tileViewManager, boardAudioManager);
        }

        public async UniTask PlaySwap(Vector2Int cellA, Vector2Int cellB)
        {
            boardAudioManager.PlayPetalSwap();
            await petalViewManager.OnSwap(cellA, cellB, layout.CellSize);
        }

        public async UniTask PlayInvalidSwapBack(Vector2Int cellA, Vector2Int cellB)
        {
            boardAudioManager.PlayInvalidSwap();
            await petalViewManager.OnSwap(cellA, cellB, layout.CellSize);
        }

        public async UniTask PlayMatch(MatchResolveResult result, IReadOnlyList<SkillUseResult> skillResults)
        {
            var skillResultsByMatch = new Dictionary<MatchGroup, SkillUseResult>(skillResults.Count);
            foreach (SkillUseResult skillResult in skillResults)
                skillResultsByMatch.Add(skillResult.MatchGroup, skillResult);

            var normalGroups = new List<MatchGroupResolveResult>();
            var tasks = new List<UniTask>();

            foreach (MatchGroupResolveResult groupResult in result.GroupResults)
            {
                if (skillResultsByMatch.TryGetValue(groupResult.SourceMatchGroup, out SkillUseResult skillResult))
                {
                    tasks.Add(skillRepresentationOrchestrator.Play(skillResult, groupResult));
                    continue;
                }

                normalGroups.Add(groupResult);
            }

            if (result.CleanedSpiderWebTileCount > 0)
                boardAudioManager.PlaySpiderWebClear();

            tasks.Add(matchPresentationCoordinator.Play(normalGroups, result.SpawnedPetals, result.AdjacentTileChanges, layout, grid));
            await UniTask.WhenAll(tasks);
        }

        public async UniTask PlayGravity(List<(Vector2Int from, Vector2Int to)> moves)
        {
            await petalViewManager.OnGravityApplied(moves, layout);

            if (moves.Count > 0)
                boardAudioManager.PlayPetalLanding();
        }

        public async UniTask PlayFill(List<Vector2Int> filledCells)
        {
            if (filledCells.Count > 0)
                boardAudioManager.PlayPetalSpawning();

            await petalViewManager.OnFilled(filledCells, layout, grid);
        }

        public async UniTask PlayShuffle(List<Vector2Int> cells)
        {
            if (cells.Count > 0)
                boardAudioManager.PlayBoardShuffle();

            await petalViewManager.OnShuffled(cells, layout, grid);
        }
    }
}
