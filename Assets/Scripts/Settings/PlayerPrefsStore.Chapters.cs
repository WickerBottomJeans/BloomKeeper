using System;
using UnityEngine;

namespace DefaultNamespace.Settings
{
    public static partial class PlayerPrefsStore
    {
        private const string LastSelectedChapterIdKey = "Chapters.LastSelectedChapterId";

        public static int? LoadLastSelectedChapterId()
        {
            if (!PlayerPrefs.HasKey(LastSelectedChapterIdKey)) return null;

            int chapterId = PlayerPrefs.GetInt(LastSelectedChapterIdKey);
            if (chapterId <= 0) throw new InvalidOperationException($"Stored chapter ID must be positive, but was {chapterId}.");
            return chapterId;
        }

        public static void SaveLastSelectedChapterId(int chapterId)
        {
            if (chapterId <= 0) throw new ArgumentOutOfRangeException(nameof(chapterId), chapterId, "Chapter ID must be positive.");

            PlayerPrefs.SetInt(LastSelectedChapterIdKey, chapterId);
            PlayerPrefs.Save();
        }
    }
}
