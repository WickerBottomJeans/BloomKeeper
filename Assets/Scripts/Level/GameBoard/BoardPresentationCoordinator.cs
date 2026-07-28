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
        private readonly BoardActionCoordinator boardActionCoordinator;
        private readonly MatchPresentationCoordinator matchPresentationCoordinator;
        private readonly SkillRepresentationOrchestrator skillRepresentationOrchestrator;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout layout;

        public BoardPresentationCoordinator(PetalViewManager petalViewManager, TileViewManager tileViewManager, SkillRepresentationOrchestrator skillRepresentationOrchestrator, BoardAudioManager boardAudioManager, BoardLayout layout, Tile[,] grid)
        {
            this.petalViewManager = petalViewManager;
            this.skillRepresentationOrchestrator = skillRepresentationOrchestrator;
            this.boardAudioManager = boardAudioManager;
            this.layout = layout;
            boardActionCoordinator = new BoardActionCoordinator(petalViewManager, boardAudioManager, layout, grid);
            matchPresentationCoordinator = new MatchPresentationCoordinator(petalViewManager, tileViewManager, boardAudioManager);
        }

        public UniTask PlaySwap(Vector2Int tileA, Vector2Int tileB)
        {
            return boardActionCoordinator.PlaySwap(tileA, tileB);
        }

        public UniTask PlayInvalidSwapBack(Vector2Int tileA, Vector2Int tileB)
        {
            return boardActionCoordinator.PlayInvalidSwapBack(tileA, tileB);
        }

        public async UniTask PlayMatch(MatchResolveResult result, IReadOnlyList<SkillUseResult> skillResults)
        {
            var skillResultsByMatch = new Dictionary<MatchGroup, SkillUseResult>(skillResults.Count);
            foreach (SkillUseResult skillResult in skillResults)
                skillResultsByMatch.Add(skillResult.MatchGroup, skillResult);

            var normalGroups = new List<MatchGroupResolveResult>();
            var skillPresentations = new List<(SkillUseResult skillResult, MatchGroupResolveResult groupResult, Dictionary<Vector2Int, ViewAccessKey> accessKeys)>();
            var accessKeySets = new List<Dictionary<Vector2Int, ViewAccessKey>>();
            var tasks = new List<UniTask>();
            var normalAccessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
            accessKeySets.Add(normalAccessKeys);

            try
            {
                matchPresentationCoordinator.AcquireSkillPetalSpawnViews(result.SpawnedPetals, normalAccessKeys);

                foreach (MatchGroupResolveResult groupResult in result.GroupResults)
                {
                    if (skillResultsByMatch.TryGetValue(groupResult.SourceMatchGroup, out SkillUseResult skillResult))
                    {
                        var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
                        accessKeySets.Add(accessKeys);
                        skillRepresentationOrchestrator.AcquireViews(skillResult, groupResult, accessKeys);
                        skillPresentations.Add((skillResult, groupResult, accessKeys));
                        continue;
                    }

                    normalGroups.Add(groupResult);
                    matchPresentationCoordinator.AcquireViews(groupResult, normalAccessKeys);
                }

                foreach (TileChange change in result.TileChanges)
                {
                    if (!change.ObstacleWasCleared) continue;
                    boardAudioManager.PlayObstacleCleared(change.Before.TileType.Value);
                    break;
                }

                foreach (var presentation in skillPresentations)
                    tasks.Add(skillRepresentationOrchestrator.Play(presentation.skillResult, presentation.groupResult, presentation.accessKeys));
                tasks.Add(matchPresentationCoordinator.Play(normalGroups, result.SpawnedPetals, result.AdjacenttileChanges, layout, normalAccessKeys));
                await UniTask.WhenAll(tasks);
            }
            finally
            {
                foreach (Dictionary<Vector2Int, ViewAccessKey> accessKeys in accessKeySets)
                {
                    var remainingAccessKeys = new List<ViewAccessKey>(accessKeys.Values);
                    foreach (ViewAccessKey accessKey in remainingAccessKeys)
                        petalViewManager.ReleaseView(accessKey);
                    accessKeys.Clear();
                }
            }
        }

        public UniTask PlayGravity(List<(Vector2Int from, Vector2Int to)> moves)
        {
            return boardActionCoordinator.PlayGravity(moves);
        }

        public UniTask PlayFill(List<Vector2Int> filledTiles)
        {
            return boardActionCoordinator.PlayFill(filledTiles);
        }

        public UniTask PlayShuffle(List<Vector2Int> tiles)
        {
            return boardActionCoordinator.PlayShuffle(tiles);
        }

        public void RefreshTile(Vector2Int tile, Petal petal)
        {
            boardActionCoordinator.RefreshTile(tile, petal);
        }
    }
}
