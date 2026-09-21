using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ScoreConfig
    {
        private readonly Dictionary<ScoreRuleType, int> baseRuleScores = new();
        private readonly Dictionary<MatchShape, int> matchShapeRuleScores = new();
        private readonly Dictionary<SpecialSkillType, int> skillRuleScores = new();

        public ScoreConfig(ScoreConfigJson scoreConfigJson)
        {
            if (scoreConfigJson == null || scoreConfigJson.rules == null || scoreConfigJson.rules.Count == 0)
                throw new InvalidOperationException("Score config must contain rules.");

            foreach (ScoreRuleJson scoreRuleJson in scoreConfigJson.rules)
            {
                if (scoreRuleJson == null) throw new InvalidOperationException("Score config contains a null rule.");
                if (!Enum.IsDefined(typeof(ScoreRuleType), scoreRuleJson.type)) throw new InvalidOperationException($"Score config contains undeclared rule type {scoreRuleJson.type}.");
                if (!Enum.IsDefined(typeof(MatchShape), scoreRuleJson.matchShape)) throw new InvalidOperationException($"Score config contains undeclared match shape {scoreRuleJson.matchShape}.");
                if (!Enum.IsDefined(typeof(SpecialSkillType), scoreRuleJson.skillType)) throw new InvalidOperationException($"Score config contains undeclared skill type {scoreRuleJson.skillType}.");
                if (scoreRuleJson.score < 0) throw new InvalidOperationException("Score config points must not be negative.");

                switch (scoreRuleJson.type)
                {
                    case ScoreRuleType.PetalCleared:
                    case ScoreRuleType.SpiderWebCleared:
                    case ScoreRuleType.CascadeDepthPetalBonus:
                        if (scoreRuleJson.matchShape != MatchShape.None || scoreRuleJson.skillType != SpecialSkillType.None)
                            throw new InvalidOperationException($"Score rule {scoreRuleJson.type} must not specify a match shape or skill.");
                        baseRuleScores.TryGetValue(scoreRuleJson.type, out int baseRuleScore);
                        baseRuleScores[scoreRuleJson.type] = baseRuleScore + scoreRuleJson.score;
                        break;
                    case ScoreRuleType.MatchShapeBonus:
                        if (scoreRuleJson.matchShape == MatchShape.None || scoreRuleJson.skillType != SpecialSkillType.None)
                            throw new InvalidOperationException("Match shape bonus must specify a match shape and no skill.");
                        matchShapeRuleScores.TryGetValue(scoreRuleJson.matchShape, out int matchShapeRuleScore);
                        matchShapeRuleScores[scoreRuleJson.matchShape] = matchShapeRuleScore + scoreRuleJson.score;
                        break;
                    case ScoreRuleType.SkillActivation:
                        if (scoreRuleJson.skillType == SpecialSkillType.None || scoreRuleJson.matchShape != MatchShape.None)
                            throw new InvalidOperationException("Skill activation bonus must specify a skill and no match shape.");
                        skillRuleScores.TryGetValue(scoreRuleJson.skillType, out int skillRuleScore);
                        skillRuleScores[scoreRuleJson.skillType] = skillRuleScore + scoreRuleJson.score;
                        break;
                    default:
                        throw new InvalidOperationException($"Score rule {scoreRuleJson.type} has no compilation behavior.");
                }
            }

            if (!baseRuleScores.ContainsKey(ScoreRuleType.PetalCleared) || !baseRuleScores.ContainsKey(ScoreRuleType.SpiderWebCleared) || !baseRuleScores.ContainsKey(ScoreRuleType.CascadeDepthPetalBonus))
                throw new InvalidOperationException("Score config requires petal clearing, web clearing, and cascade bonus rules.");
        }

        public int GetRuleScore(ScoreRuleType scoreRuleType)
        {
            return baseRuleScores[scoreRuleType];
        }

        public int GetRuleScore(ScoreRuleType scoreRuleType, MatchShape matchShape)
        {
            if (scoreRuleType != ScoreRuleType.MatchShapeBonus) throw new ArgumentException("Match shape lookup requires a match shape bonus rule.", nameof(scoreRuleType));
            if (!Enum.IsDefined(typeof(MatchShape), matchShape)) throw new ArgumentOutOfRangeException(nameof(matchShape));
            return matchShapeRuleScores.TryGetValue(matchShape, out int matchShapeRuleScore) ? matchShapeRuleScore : 0;
        }

        public int GetRuleScore(ScoreRuleType scoreRuleType, SpecialSkillType skillType)
        {
            if (scoreRuleType != ScoreRuleType.SkillActivation) throw new ArgumentException("Skill lookup requires a skill activation rule.", nameof(scoreRuleType));
            if (!Enum.IsDefined(typeof(SpecialSkillType), skillType)) throw new ArgumentOutOfRangeException(nameof(skillType));
            return skillRuleScores.TryGetValue(skillType, out int skillRuleScore) ? skillRuleScore : 0;
        }
    }
}
