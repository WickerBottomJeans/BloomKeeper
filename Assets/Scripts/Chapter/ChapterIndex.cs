using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    /// <summary>
    /// Lists the available chapters and the metadata needed to locate and present them.
    /// </summary>
    public class ChapterIndex
    {
        public List<ChapterIndexEntry> chapters;

        /// <summary>
        /// [Duong] Returns the indexed entry for a chapter ID.
        /// </summary>
        public ChapterIndexEntry GetEntry(int chapterId)
        {
            foreach (ChapterIndexEntry chapter in chapters)
                if (chapter.chapterId == chapterId)
                    return chapter;

            throw new KeyNotFoundException($"Chapter index has no entry for chapter {chapterId}.");
        }

        /// <summary>
        /// Returns the chapter with the highest unlock level not above the given level
        /// </summary>
        public ChapterIndexEntry GetLatestUnlockedEntry(int highestUnlockedLevel)
        {
            ChapterIndexEntry latestUnlockedChapter = null;
            foreach (ChapterIndexEntry chapter in chapters)
            {
                if (chapter.unlockLevelId > highestUnlockedLevel) continue;
                if (latestUnlockedChapter == null || chapter.unlockLevelId > latestUnlockedChapter.unlockLevelId)
                    latestUnlockedChapter = chapter;
            }

            return latestUnlockedChapter ?? throw new InvalidOperationException($"No chapter is unlocked for highest unlocked level {highestUnlockedLevel}.");
        }
    }

    /// <summary>
    /// Describes one indexed chapter, including its unlock requirement and content references.
    /// </summary>
    public class ChapterIndexEntry
    {
        public int chapterId;
        public string displayName;
        public string description;
        public string configPath;
        public string chooserImageAddress;
        public string downloadLabel;
        public int unlockLevelId;
    }
}
