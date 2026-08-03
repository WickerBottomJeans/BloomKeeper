using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    public class ChapterDownloadPreferenceStore
    {
        private const string FileName = "chapter_download_preferences.json";

        public IReadOnlyList<int> GetChapterIds()
        {
            return LoadChapterIds();
        }

        public void AddChapter(int chapterId)
        {
            List<int> chapterIds = LoadChapterIds();
            if (chapterIds.Contains(chapterId)) return;
            chapterIds.Add(chapterId);
            chapterIds.Sort();
            SaveChapterIds(chapterIds);
        }

        public void RemoveChapter(int chapterId)
        {
            List<int> chapterIds = LoadChapterIds();
            if (!chapterIds.Remove(chapterId)) return;
            SaveChapterIds(chapterIds);
        }

        private static List<int> LoadChapterIds()
        {
            string path = GetPath();
            if (!File.Exists(path)) return new List<int>();

            List<int> chapterIds;
            try
            {
                chapterIds = JsonConvert.DeserializeObject<List<int>>(File.ReadAllText(path));
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Chapter download preferences contain invalid JSON: {path}", exception);
            }

            if (chapterIds == null) throw new InvalidDataException($"Chapter download preferences must contain an array: {path}");
            var uniqueChapterIds = new HashSet<int>();
            foreach (int chapterId in chapterIds)
            {
                if (!uniqueChapterIds.Add(chapterId)) throw new InvalidDataException($"Chapter download preferences contain chapter {chapterId} more than once: {path}");
            }

            return chapterIds.OrderBy(chapterId => chapterId).ToList();
        }

        private static void SaveChapterIds(List<int> chapterIds)
        {
            File.WriteAllText(GetPath(), JsonConvert.SerializeObject(chapterIds, Formatting.Indented));
        }

        private static string GetPath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

    }
}
