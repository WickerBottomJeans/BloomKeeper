using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ScoreConfigJson
    {
        public List<ScoreRuleJson> rules;
    }

    public class ScoreRuleJson
    {
        public ScoreRuleType type;
        public MatchShape matchShape;
        public SpecialSkillType skillType;
        public int score;
    }

    public class StarScoreThresholdJson
    {
        public int starCount;
        public int score;
    }
}
