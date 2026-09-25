#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace DefaultNamespace.Editor
{
    /// <summary>
    /// Checks level data before the editor writes a playable config.
    /// </summary>
    public static class LevelDefinitionValidator
    {
        public static List<string> ValidateLevelData(LevelData levelData)
        {
            var errors = new List<string>();
            if (levelData == null)
            {
                errors.Add("The level document is empty.");
                return errors;
            }

            if (levelData.boardWidth <= 0 || levelData.boardHeight <= 0) errors.Add("Board width and height must be positive.");
            if (levelData.tiles == null || levelData.tiles.Count != (long)levelData.boardWidth * levelData.boardHeight) errors.Add("The tile count must equal board width × height.");
            if (levelData.nextLevelId == levelData.levelId) errors.Add("The next level cannot be this level.");

            int webTileCount = 0;
            int playableTileCount = 0;
            if (levelData.tiles != null)
            {
                for (int index = 0; index < levelData.tiles.Count; index++)
                {
                    TileData tileData = levelData.tiles[index];
                    string location = levelData.boardWidth > 0 ? $"Tile (column {index % levelData.boardWidth + 1}, row {index / levelData.boardWidth + 1} from top)" : $"Tile {index}";
                    if (tileData == null)
                    {
                        errors.Add($"{location}: tile data is null.");
                        continue;
                    }

                    if (!Enum.IsDefined(typeof(PetalType), tileData.petalType)) errors.Add($"{location}: unknown flower type.");
                    if (!Enum.IsDefined(typeof(SpecialSkillType), tileData.skillType)) errors.Add($"{location}: unknown skill type.");
                    if (!tileData.isVoid && !Enum.IsDefined(typeof(TileType), tileData.type)) errors.Add($"{location}: unknown tile type.");
                    if (tileData.webLevel < 0) errors.Add($"{location}: web layers cannot be negative.");
                    if ((tileData.isVoid || tileData.type != TileType.Web) && tileData.webLevel != 0) errors.Add($"{location}: only web tiles may have web layers.");

                    bool canContainPetal = !tileData.isVoid && (tileData.type == TileType.Normal || tileData.type == TileType.Web && tileData.webLevel == 0);
                    if (canContainPetal) playableTileCount++;
                    if (!canContainPetal && (tileData.petalType != PetalType.None || tileData.skillType != SpecialSkillType.None)) errors.Add($"{location}: this tile cannot contain a flower or skill.");
                    if (tileData.petalType == PetalType.None && tileData.skillType != SpecialSkillType.None) errors.Add($"{location}: choose a fixed flower for the skill; random initialization does not preserve a configured skill.");
                    if (!tileData.isVoid && tileData.type == TileType.Web && tileData.webLevel > 0) webTileCount++;
                }
            }
            if (playableTileCount == 0) errors.Add("The board needs at least one tile that can initially contain a flower.");

            ValidateLevelObjectives(levelData.objectives, webTileCount, errors);
            ValidateLevelConstrainers(levelData.constrainers, errors);
            ValidateStarThresholds(levelData.starScoreThresholds, errors);

            if (levelData.allowedBoosters == null) errors.Add("The allowed boosters list is missing.");
            else
            {
                var boosterTypes = new HashSet<BoosterType>();
                foreach (BoosterType boosterType in levelData.allowedBoosters)
                {
                    if (!Enum.IsDefined(typeof(BoosterType), boosterType)) errors.Add($"Unknown booster type: {boosterType}.");
                    if (!boosterTypes.Add(boosterType)) errors.Add($"Duplicate booster: {boosterType}.");
                }
            }
            return errors;
        }

        private static void ValidateLevelObjectives(List<ObjectiveJson> objectives, int webTileCount, List<string> errors)
        {
            if (objectives == null || objectives.Count == 0)
            {
                errors.Add("Add at least one objective.");
                return;
            }
            for (int index = 0; index < objectives.Count; index++)
            {
                ObjectiveJson objectiveJson = objectives[index];
                string label = $"Objective {index + 1}";
                if (objectiveJson == null || !Enum.IsDefined(typeof(ObjectiveType), objectiveJson.type))
                {
                    errors.Add($"{label}: missing data or unknown objective type.");
                    continue;
                }
                switch (objectiveJson.type)
                {
                    case ObjectiveType.Match:
                        if (objectiveJson.petals == null || objectiveJson.petals.Count == 0) errors.Add($"{label}: add at least one flower goal.");
                        else
                        {
                            var petalTypes = new HashSet<PetalType>();
                            foreach (PetalGoal petalGoal in objectiveJson.petals)
                            {
                                if (petalGoal == null)
                                {
                                    errors.Add($"{label}: flower goal is null.");
                                    continue;
                                }
                                if (!Enum.IsDefined(typeof(PetalType), petalGoal.petalType) || petalGoal.petalType == PetalType.None) errors.Add($"{label}: choose a concrete flower type.");
                                if (!petalTypes.Add(petalGoal.petalType)) errors.Add($"{label}: duplicate flower goal for {petalGoal.petalType}.");
                                if (petalGoal.amount <= 0) errors.Add($"{label}: flower amounts must be positive.");
                            }
                        }
                        break;
                    case ObjectiveType.ClearSpiderWeb:
                        if (objectiveJson.spiderWebsToClear <= 0 || objectiveJson.spiderWebsToClear > webTileCount) errors.Add($"{label}: web target must be between 1 and {webTileCount}, the number of tiles with webs (not layers).");
                        break;
                    default:
                        errors.Add($"{label}: {objectiveJson.type} has no supported authoring behavior. The current game does not implement Butterfly objectives.");
                        break;
                }
            }
        }

        private static void ValidateLevelConstrainers(List<ConstrainerJson> constrainers, List<string> errors)
        {
            if (constrainers == null)
            {
                errors.Add("The constraints list is missing.");
                return;
            }
            var constrainerTypes = new HashSet<ConstrainerType>();
            foreach (ConstrainerJson constrainerJson in constrainers)
            {
                if (constrainerJson == null || !Enum.IsDefined(typeof(ConstrainerType), constrainerJson.type))
                {
                    errors.Add("A constraint has missing data or an unknown type.");
                    continue;
                }
                if (!constrainerTypes.Add(constrainerJson.type)) errors.Add($"Duplicate constraint: {constrainerJson.type}.");
                double limit = constrainerJson.type == ConstrainerType.MoveLimit ? constrainerJson.moveLimit : constrainerJson.timeLimitSeconds;
                if (double.IsNaN(limit) || double.IsInfinity(limit) || limit <= 0) errors.Add($"{constrainerJson.type}: limit must be finite and positive.");
                if (constrainerJson.warningAtRemaining <= 0 || constrainerJson.warningAtRemaining >= limit) errors.Add($"{constrainerJson.type}: warning must be positive and lower than the limit.");
            }
        }

        private static void ValidateStarThresholds(List<StarScoreThresholdJson> starScoreThresholds, List<string> errors)
        {
            if (starScoreThresholds == null || starScoreThresholds.Count == 0)
            {
                errors.Add("Add at least one star threshold.");
                return;
            }
            var sortedThresholds = new List<StarScoreThresholdJson>();
            foreach (StarScoreThresholdJson thresholdJson in starScoreThresholds)
            {
                if (thresholdJson == null) errors.Add("A star threshold is null.");
                else sortedThresholds.Add(thresholdJson);
            }
            sortedThresholds.Sort((left, right) => left.starCount.CompareTo(right.starCount));
            int previousStars = 0;
            int previousScore = 0;
            foreach (StarScoreThresholdJson thresholdJson in sortedThresholds)
            {
                if (thresholdJson.starCount <= previousStars || thresholdJson.score <= previousScore) errors.Add("Star counts and scores must be positive and strictly increase together.");
                previousStars = thresholdJson.starCount;
                previousScore = thresholdJson.score;
            }
        }
    }
}
#endif
