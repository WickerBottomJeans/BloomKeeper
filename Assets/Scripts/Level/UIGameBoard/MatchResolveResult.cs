using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class MatchResolveResult
    {
        public List<MatchGroupResolveResult> GroupResults;
        public List<PetalType> ClearedPetalTypes;
        public List<SkillActivation> SkillActivations;
        public List<SkillPetalSpawn> SpawnedPetals;
        public List<Vector2Int> AdjacentTileChanges;
        //TODO: Coupling here. i knew it, couldnt find a way to isolate it out tho
        public int CleanedSpiderWebTileCount;

        public MatchResolveResult(List<MatchGroupResolveResult> groupResults, List<PetalType> clearedPetalTypes, List<SkillActivation> skillActivations, List<SkillPetalSpawn> spawnedPetals, List<Vector2Int> adjacentTileChanges, int cleanedSpiderWebTileCount)
        {
            GroupResults = groupResults;
            ClearedPetalTypes = clearedPetalTypes;
            SkillActivations = skillActivations;
            SpawnedPetals = spawnedPetals;
            AdjacentTileChanges = adjacentTileChanges;
            CleanedSpiderWebTileCount = cleanedSpiderWebTileCount;
        }
    }
}
