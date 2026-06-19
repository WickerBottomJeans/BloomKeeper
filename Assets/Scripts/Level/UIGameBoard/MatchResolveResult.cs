using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class MatchResolveResult
{
    public List<Vector2Int> ClearedPositions;
    public List<PetalType> ClearedPetalTypes;
    public List<SkillActivation> SkillActivations;
    //TODO: why dont we just use Petal here?
    public List<(Vector2Int Position, PetalType PetalType, SpecialSkillType SkillType)> SpawnedPetals;
    public List<Vector2Int> ChangedTiles;

    public MatchResolveResult(
        List<Vector2Int> clearedPositions,
        List<PetalType> clearedPetalTypes,
        List<SkillActivation> skillActivations,
        List<(Vector2Int, PetalType, SpecialSkillType)> spawnedPetals,
        List<Vector2Int> changedTiles)
    {
        ClearedPositions = clearedPositions;
        ClearedPetalTypes = clearedPetalTypes;
        SkillActivations = skillActivations;
        SpawnedPetals = spawnedPetals;
        ChangedTiles = changedTiles;
    }
}
}
