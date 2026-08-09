using System.Collections.Generic;
using Boosters;
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
        private readonly BoosterRepresentationOrchestrator boosterRepresentationOrchestrator;
        private readonly BoardAudioManager boardAudioManager;
        private readonly BoardLayout layout;

        public BoardPresentationCoordinator(PetalViewManager petalViewManager, TileViewManager tileViewManager, SkillRepresentationOrchestrator skillRepresentationOrchestrator, BoosterRepresentationOrchestrator boosterRepresentationOrchestrator, BoardAudioManager boardAudioManager, BoardLayout layout, Tile[,] grid)
        {
            this.petalViewManager = petalViewManager;
            this.skillRepresentationOrchestrator = skillRepresentationOrchestrator;
            this.boosterRepresentationOrchestrator = boosterRepresentationOrchestrator;
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

        public UniTask PlayInitialMatch(MatchResolveResult result)
        {
            return PlayResolution(result, System.Array.Empty<SkillUseResult>());
        }

        public async UniTask PlaySkillWave(SkillResolutionWave skillWave)
        {
            using var audioScope = new AudioPlaybackScope();
            await PlayResolution(skillWave.Resolution, skillWave.SkillResults, audioScope);
        }

        public async UniTask PlayBooster(BoosterUseResult boosterUseResult, MatchResolveResult resolution)
        {
            var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
            try
            {
                boosterRepresentationOrchestrator.AcquireVitalViews(boosterUseResult, resolution, accessKeys);
                await boosterRepresentationOrchestrator.Play(boosterUseResult, resolution, accessKeys);
            }
            finally
            {
                var remainingAccessKeys = new List<ViewAccessKey>(accessKeys.Values);
                foreach (ViewAccessKey accessKey in remainingAccessKeys)
                    petalViewManager.ReleaseView(accessKey);
                accessKeys.Clear();
            }
        }

        public void ShowBoosterTargets(BoosterType boosterType, IReadOnlyList<Vector2Int> positions)
        {
            boosterRepresentationOrchestrator.ShowBoosterTargets(boosterType, positions);
        }

        public void HideBoosterTargets()
        {
            boosterRepresentationOrchestrator.HideBoosterTargets();
        }

        private async UniTask PlayResolution(MatchResolveResult result, IReadOnlyList<SkillUseResult> skillResults, AudioPlaybackScope audioScope = null)
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
                foreach (MatchGroupResolveResult groupResult in result.GroupResults)
                {
                    if (skillResultsByMatch.TryGetValue(groupResult.SourceMatchGroup, out SkillUseResult skillResult))
                    {
                        var accessKeys = new Dictionary<Vector2Int, ViewAccessKey>();
                        accessKeySets.Add(accessKeys);
                        skillPresentations.Add((skillResult, groupResult, accessKeys));
                        continue;
                    }

                    normalGroups.Add(groupResult);
                }

                foreach (var presentation in skillPresentations)
                    skillRepresentationOrchestrator.AcquireVitalViews(presentation.skillResult, presentation.groupResult, presentation.accessKeys);

                matchPresentationCoordinator.AcquireSkillPetalSpawnViews(result.SpawnedPetals, normalAccessKeys);

                foreach (MatchGroupResolveResult normalGroup in normalGroups)
                    matchPresentationCoordinator.AcquireViews(normalGroup, normalAccessKeys);

                foreach (TileChange change in result.TileChanges)
                {
                    if (!change.ObstacleWasCleared) continue;
                    boardAudioManager.PlayObstacleCleared(change.Before.TileType.Value);
                    break;
                }

                foreach (var presentation in skillPresentations)
                    tasks.Add(skillRepresentationOrchestrator.Play(presentation.skillResult, presentation.groupResult, presentation.accessKeys, audioScope));
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
