using UnityEngine;

namespace DefaultNamespace.Settings
{
    public static partial class PlayerPrefsStore
    {
        private const string AutomaticGooglePlayReviewAttemptedKey = "HasAttemptedAutomaticGooglePlayReview";

        public static bool LoadAutomaticGooglePlayReviewAttempted()
        {
            return PlayerPrefs.GetInt(AutomaticGooglePlayReviewAttemptedKey, 0) == 1;
        }

        public static void SaveAutomaticGooglePlayReviewAttempted()
        {
            PlayerPrefs.SetInt(AutomaticGooglePlayReviewAttemptedKey, 1);
            PlayerPrefs.Save();
        }
    }
}
