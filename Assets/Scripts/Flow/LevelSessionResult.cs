namespace DefaultNamespace
{
    /// <summary>
    /// The result generated locally by the game after finishing a level
    /// </summary>
    public class LevelSessionResult
    {
        public int LevelId { get; }
        public bool DidWin { get; }
        public int Score { get; }
        public int Stars { get; }
        public int StarCap { get; }
        public string FailureMessage { get; }

        public LevelSessionResult(int levelId, bool didWin, int score, int stars, int starCap, string failureMessage)
        {
            LevelId = levelId;
            DidWin = didWin;
            Score = score;
            Stars = stars;
            StarCap = starCap;
            FailureMessage = failureMessage;
        }
    }
}
