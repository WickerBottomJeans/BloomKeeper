using System;
using System.Collections.Generic;
using DefaultNamespace;

namespace DefaultNamespace.UI
{
    public class LevelUIInitData
    {
        public IReadOnlyList<ObjectiveViewData> Objectives { get; }
        public IReadOnlyList<ConstrainerViewData> Constrainers { get; }
        public ScoreViewData Score { get; }
        public IReadOnlyList<BoosterViewData> AvailableBoosters { get; }

        public LevelUIInitData(IReadOnlyList<ObjectiveViewData> objectives, IReadOnlyList<ConstrainerViewData> constrainers, ScoreViewData score, IReadOnlyList<BoosterViewData> availableBoosters)
        {
            Objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
            Constrainers = constrainers ?? throw new ArgumentNullException(nameof(constrainers));
            Score = score ?? throw new ArgumentNullException(nameof(score));
            AvailableBoosters = availableBoosters ?? throw new ArgumentNullException(nameof(availableBoosters));
        }
    }

    public class ScoreViewData
    {
        public int TargetScore { get; }
        public IReadOnlyList<int> MilestoneScores { get; }
        public int StarCap { get; }

        public ScoreViewData(int targetScore, IReadOnlyList<int> milestoneScores, int starCap)
        {
            TargetScore = targetScore;
            MilestoneScores = milestoneScores ?? throw new ArgumentNullException(nameof(milestoneScores));
            StarCap = starCap;
        }
    }
}
