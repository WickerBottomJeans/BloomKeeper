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

        public void AcquireSkillPetalSpawnViews(IReadOnlyList<SkillPetalSpawn> skillPetalSpawns, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            foreach (SkillPetalSpawn spawn in skillPetalSpawns)
            {
                foreach (Vector2Int position in spawn.ContributorPositions)
                {
                    if (accessKeys.ContainsKey(position)) continue;
                    if (petalViewManager.TryAcquireView(position, nameof(MatchPresentationCoordinator), out ViewAccessKey accessKey))
                        accessKeys.Add(position, accessKey);
                }
            }
        }

        public void AcquireViews(MatchGroupResolveResult groupResult, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var positions = new List<Vector2Int>();
            AddNormalRemovals(groupResult, positions);
            positions.AddRange(groupResult.GetSkillTriggerPositions());
            foreach (Vector2Int position in positions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (petalViewManager.TryAcquireView(position, nameof(MatchPresentationCoordinator), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
            }
        }

        public async UniTask Play(IReadOnlyList<MatchGroupResolveResult> groupResults, IReadOnlyList<SkillPetalSpawn> skillPetalSpawns, IReadOnlyList<TileChange> adjacenttileChanges, BoardLayout boardLayout, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var normalRemovals = new List<Vector2Int>();
            var normalTileChanges = new List<TileChange>(adjacenttileChanges.Count);
            var tasks = new List<UniTask>();

            foreach (TileChange change in adjacenttileChanges)
            {
                if (change.ObstacleLayerChanged)
                    normalTileChanges.Add(change);
            }

            foreach (MatchGroupResolveResult groupResult in groupResults)
            {
                AddNormalRemovals(groupResult, normalRemovals);
                AddNormalTileChanges(groupResult, normalTileChanges);
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
            normalRemovals.RemoveAll(position => !accessKeys.ContainsKey(position));
            var ownedTriggeredSkillPositions = new List<Vector2Int>();
            foreach (MatchGroupResolveResult groupResult in groupResults)
            {
                foreach (Vector2Int position in groupResult.GetSkillTriggerPositions())
                {
                    if (accessKeys.ContainsKey(position))
                        ownedTriggeredSkillPositions.Add(position);
                }
            }

            tasks.Add(tileViewManager.PlayTileChanges(normalTileChanges));
            tasks.Add(petalViewManager.PlayAboutToExecuteShake(ownedTriggeredSkillPositions, accessKeys));
            tasks.Add(petalViewManager.PlayNormalRemovals(normalRemovals, accessKeys));
            tasks.Add(petalViewManager.PlaySkillPetalCreations(skillPetalSpawns, boardLayout, accessKeys));
            await UniTask.WhenAll(tasks);
        }

        private static void AddNormalRemovals(MatchGroupResolveResult groupResult, List<Vector2Int> normalRemovals)
        {
            if (groupResult.SourceMatchGroup.IsFromSkillCombo) return;

            foreach (var impact in groupResult.TileChanges)
            {
                if (!impact.PetalWasRemoved || impact.RemovedSkillType != SpecialSkillType.None) continue;
                normalRemovals.Add(impact.Position);
            }
        }

        private static void AddNormalTileChanges(MatchGroupResolveResult groupResult, List<TileChange> normalTileChanges)
        {
            if (groupResult.SourceMatchGroup.IsFromSkillCombo) return;

            foreach (var impact in groupResult.TileChanges)
            {
                if (impact.ObstacleLayerChanged)
                    normalTileChanges.Add(impact);
            }
        }
    }
}
