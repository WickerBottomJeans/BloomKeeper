using System;
using System.Collections.Generic;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class ScoreManager
    {
        private readonly List<ScoreRuleJson> rules;
        private readonly List<StarScoreThresholdJson> starScoreThresholds;

        public event Action<int, int> OnScoreChanged;
        public int CurrentScore { get; private set; }

        public ScoreManager(List<StarScoreThresholdJson> starScoreThresholds)
        {
            rules = ScoreLoader.Load().rules;
            this.starScoreThresholds = starScoreThresholds;
        }

        public int Apply(BoardResolvedEvent e)
        {
            if (!e.CountsForScore) return 0;

            int delta = CalculateScore(e);
            if (delta <= 0) return 0;

            CurrentScore += delta;
            OnScoreChanged?.Invoke(CurrentScore, CalculateStars());
            return delta;
        }

        public int CalculateStars()
        {
            int stars = 0;
            foreach (StarScoreThresholdJson threshold in starScoreThresholds)
            {
                if (CurrentScore >= threshold.score && threshold.starCount > stars)
                    stars = threshold.starCount;
            }

            return stars;
        }

        private int CalculateScore(BoardResolvedEvent e)
        {
            int clearedPetalCount = 0;
            int clearedSpiderWebCount = 0;
            foreach (TileChange change in e.Result.TileChanges)
            {
                if (change.PetalWasRemoved)
                    clearedPetalCount++;
                if (change.Before.TileType == TileType.Web && change.ObstacleWasCleared)
                    clearedSpiderWebCount++;
            }

            int score = clearedPetalCount * GetRuleScore(ScoreRuleType.PetalCleared);
            score += clearedPetalCount * e.CascadeDepth * GetRuleScore(ScoreRuleType.CascadeDepthPetalBonus);
            score += clearedSpiderWebCount * GetRuleScore(ScoreRuleType.SpiderWebCleared);

            foreach (var groupResult in e.Result.GroupResults)
            {
                score += GetRuleScore(ScoreRuleType.MatchShapeBonus, groupResult.SourceMatchGroup.Shape);
                SpecialSkillType skillType = groupResult.SourceMatchGroup.Causer?.Skill ?? SpecialSkillType.None;
                if (skillType != SpecialSkillType.None) score += GetRuleScore(ScoreRuleType.SkillActivation, skillType);
            }

            return score;
        }

        private int GetRuleScore(ScoreRuleType type)
        {
            int score = 0;
            foreach (ScoreRuleJson rule in rules)
            {
                if (rule.type == type)
                    score += rule.score;
            }

            return score;
        }

        private int GetRuleScore(ScoreRuleType type, MatchShape matchShape)
        {
            int score = 0;
            foreach (ScoreRuleJson rule in rules)
            {
                if (rule.type == type && rule.matchShape == matchShape)
                    score += rule.score;
            }

            return score;
        }

        private int GetRuleScore(ScoreRuleType type, SpecialSkillType skillType)
        {
            int score = 0;
            foreach (ScoreRuleJson rule in rules)
            {
                if (rule.type == type && rule.skillType == skillType)
                    score += rule.score;
            }

            return score;
        }
    }
}
