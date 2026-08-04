using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ChapterIndex
    {
        public List<ChapterIndexEntry> chapters;
    }

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
