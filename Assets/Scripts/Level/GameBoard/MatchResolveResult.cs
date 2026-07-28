using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class MatchResolveResult
    {
        public List<MatchGroupResolveResult> GroupResults;
        public List<SkillPetalSpawn> SpawnedPetals;
        public IReadOnlyList<TileChange> AdjacenttileChanges { get; }
        public IReadOnlyList<TileChange> TileChanges { get; }

        public MatchResolveResult(List<MatchGroupResolveResult> groupResults, List<SkillPetalSpawn> spawnedPetals, IReadOnlyList<TileChange> adjacenttileChanges)
        {
            GroupResults = groupResults;
            SpawnedPetals = spawnedPetals;
            AdjacenttileChanges = adjacenttileChanges;

            var tileChanges = new List<TileChange>();
            foreach (MatchGroupResolveResult groupResult in groupResults)
                tileChanges.AddRange(groupResult.TileChanges);
            tileChanges.AddRange(adjacenttileChanges);
            this.TileChanges = tileChanges;
        }
    }
}
