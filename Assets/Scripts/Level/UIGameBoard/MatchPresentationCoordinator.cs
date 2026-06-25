using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Skills;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class MatchPresentationCoordinator
    {
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly SkillRepresentationOrchestrator skillRepresentationOrchestrator;
        private readonly BoardLayout layout;

        public MatchPresentationCoordinator(PetalViewManager petalViewManager, TileViewManager tileViewManager, SkillRepresentationOrchestrator skillRepresentationOrchestrator, BoardLayout layout)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.skillRepresentationOrchestrator = skillRepresentationOrchestrator;
            this.layout = layout;
        }

        public async UniTask Play(MatchResolveResult result, IReadOnlyList<SkillUseResult> skillResults, BoardCell[,] grid)
        {
            var skillResultsByMatch = new Dictionary<MatchGroup, SkillUseResult>(skillResults.Count);
            foreach (SkillUseResult skillResult in skillResults)
                skillResultsByMatch.Add(skillResult.MatchGroup, skillResult);

            var normalRemovals = new List<Vector2Int>();
            var normalTileChanges = new List<Vector2Int>(result.AdjacentTileChanges);
            var tasks = new List<UniTask>();

            foreach (MatchGroupResolveResult groupResult in result.GroupResults)
            {
                if (skillResultsByMatch.TryGetValue(groupResult.SourceMatchGroup, out SkillUseResult skillResult))
                {
                    tasks.Add(skillRepresentationOrchestrator.Play(skillResult, groupResult));
                    continue;
                }

                AddNormalRemovals(groupResult, normalRemovals);
                AddNormalTileChanges(groupResult, normalTileChanges);
                if (groupResult.TriggeredSkillPositions.Count > 0)
                    tasks.Add(petalViewManager.PlayAboutToExecuteShake(groupResult.TriggeredSkillPositions));
            }

            tasks.Add(tileViewManager.PlayTileChanges(normalTileChanges, grid));
            tasks.Add(petalViewManager.PlayNormalRemovals(normalRemovals));
            await UniTask.WhenAll(tasks);
            await petalViewManager.PlaySpawnedPetals(result.SpawnedPetals, layout);
        }

        private static void AddNormalRemovals(MatchGroupResolveResult groupResult, List<Vector2Int> normalRemovals)
        {
            if (groupResult.SourceMatchGroup.IsFromSkillCombo) return;

            foreach (var impact in groupResult.Impacts)
            {
                Petal removedPetal = impact.Outcome.RemovedPetal;
                if (removedPetal == null || removedPetal.Skill != SpecialSkillType.None) continue;
                normalRemovals.Add(impact.Position);
            }
        }

        private static void AddNormalTileChanges(MatchGroupResolveResult groupResult, List<Vector2Int> normalTileChanges)
        {
            if (groupResult.SourceMatchGroup.IsFromSkillCombo) return;

            foreach (var impact in groupResult.Impacts)
            {
                if (impact.Outcome.TileChanged)
                    normalTileChanges.Add(impact.Position);
            }
        }
    }
}
