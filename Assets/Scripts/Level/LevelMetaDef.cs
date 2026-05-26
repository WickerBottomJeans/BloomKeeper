using System.Collections.Generic;

namespace DefaultNamespace
{
    public class LevelMeta
    {
        public int levelId;
        public string levelName;
        
        /// <summary>
        /// Use for scrollrect placement
        /// </summary>
        public float pixelX;
        public float pixelY;
    }
    
    public class LevelMetaCollection
    {
        public float referenceScreenWidth;
        public List<LevelMeta> levels;
    }
}