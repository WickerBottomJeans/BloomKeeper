using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class MatchPresentationCoordinator
    {
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardAudioManager boardAudioManager;

        public MatchPresentationCoordinator(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardAudioManager boardAudioManager)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardAudioManager = boardAudioManager;
        }

        public async UniTask Play(IReadOnlyList<MatchGroupResolveResult> groupResults, IReadOnlyList<SkillPetalSpawn> skillPetalSpawns, IReadOnlyList<Vector2Int> adjacentTileChanges, BoardLayout boardLayout, BoardCell[,] grid)
        {
            var normalRemovals = new List<Vector2Int>();
            var normalTileChanges = new List<Vector2Int>(adjacentTileChanges);
            var tasks = new List<UniTask>();

            foreach (MatchGroupResolveResult groupResult in groupResults)
            {
                AddNormalRemovals(groupResult, normalRemovals);
                AddNormalTileChanges(groupResult, normalTileChanges);
                if (groupResult.TriggeredSkillPositions.Count > 0)
                    tasks.Add(petalViewManager.PlayAboutToExecuteShake(groupResult.TriggeredSkillPositions));
            }

            if (normalRemovals.Count > 0)
                boardAudioManager.PlayMatchClear();

            var skillCreationPositions = new HashSet<Vector2Int>();
            foreach (SkillPetalSpawn spawn in skillPetalSpawns)
            {
                foreach (Vector2Int position in spawn.ContributorPositions)
                    skillCreationPositions.Add(position);
            }

            normalRemovals.RemoveAll(skillCreationPositions.Contains);

            tasks.Add(tileViewManager.PlayTileChanges(normalTileChanges, grid));
            tasks.Add(petalViewManager.PlayNormalRemovals(normalRemovals));
            tasks.Add(petalViewManager.PlaySkillPetalCreations(skillPetalSpawns, boardLayout));
            await UniTask.WhenAll(tasks);
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
