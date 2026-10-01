using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public static class TutorialProgressContract
    {
        public const int CurrentSchemaVersion = 1;
        public const string HomeIntroductionTutorialId = "home-introduction";

        private static readonly HashSet<string> RecognizedTutorialIds = new HashSet<string> { HomeIntroductionTutorialId };

        public static void ValidateTutorialId(string tutorialId)
        {
            if (tutorialId == null || !RecognizedTutorialIds.Contains(tutorialId)) throw new ArgumentException($"Unknown tutorial ID '{tutorialId}'.", nameof(tutorialId));
        }

        public static void ValidateTutorialProgress(PlayerTutorialProgressData playerTutorialProgressData)
        {
            if (playerTutorialProgressData == null) throw new InvalidOperationException("Tutorial progress is missing.");
            if (playerTutorialProgressData.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"Unsupported tutorial progress version {playerTutorialProgressData.schemaVersion}.");
            if (playerTutorialProgressData.completedTutorialIds == null) throw new InvalidOperationException("Completed tutorial IDs are missing.");
            foreach (string tutorialId in playerTutorialProgressData.completedTutorialIds) ValidateTutorialId(tutorialId);
        }
    }
}
