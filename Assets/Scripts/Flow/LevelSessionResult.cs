using System;

namespace DefaultNamespace
{
    /// <summary>
    /// The result generated locally by the game after finishing a level
    /// </summary>
    public class LevelSessionResult
    {
        public int LevelId { get; }
        public string AttemptId { get; }
        public bool DidWin { get; }
        public int Score { get; }
        public int Stars { get; }
        public int StarCap { get; }
        public string FailureMessage { get; }

        public LevelSessionResult(int levelId, string attemptId, bool didWin, int score, int stars, int starCap, string failureMessage)
        {
            if (string.IsNullOrWhiteSpace(attemptId)) throw new ArgumentException("A level session result requires an attempt ID.", nameof(attemptId));

            LevelId = levelId;
            AttemptId = attemptId;
            DidWin = didWin;
            Score = score;
            Stars = stars;
            StarCap = starCap;
            FailureMessage = failureMessage;
        }
    }
}
