using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ChapterDefinition
    {
        public int schemaVersion;
        public int chapterId;
        public string chapterName;
        public string downloadLabel;
        public string topperPrefabAddress;
        public string bottomNavigationPrefabAddress;
        public string levelButtonPrefabAddress;
        public List<ChapterLevelDisplayData> levels;
        public List<ChapterBackgroundChunkData> backgroundChunks;
    }

    public class ChapterLevelDisplayData
    {
        public int levelId;
        public string levelName;
        public float pixelX;
        public float pixelY;
    }

    public class ChapterBackgroundChunkData
    {
        public string address;
        public string previewAddress;
        public int width;
        public int height;
    }
}
