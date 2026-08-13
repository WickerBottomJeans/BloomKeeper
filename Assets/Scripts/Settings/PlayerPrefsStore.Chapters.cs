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
            return chapterId;
        }

        public static void SaveLastSelectedChapterId(int chapterId)
        {
            PlayerPrefs.SetInt(LastSelectedChapterIdKey, chapterId);
            PlayerPrefs.Save();
        }
    }
}
