using System.Collections.Generic;

namespace DefaultNamespace
{
    public class LevelData
    {
        public int levelId;
        public int boardWidth;
        public int boardHeight;
        public List<TileData> tiles;
        public List<ObjectiveJson> objectives;
    }
}