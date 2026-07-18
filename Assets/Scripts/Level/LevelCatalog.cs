using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public class LevelCatalog
    {
        private readonly IReadOnlyList<LevelMeta> levels;

        public LevelCatalog(LevelMetaCollection metaCollection)
        {
            if (metaCollection == null)
                throw new ArgumentNullException(nameof(metaCollection));
            levels = metaCollection.levels ?? throw new ArgumentException("Level metadata must contain an advertised level collection.", nameof(metaCollection));
        }

        public bool TryGetNextLevelId(int currentLevelId, out int nextLevelId)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].levelId != currentLevelId) continue;

                if (i == levels.Count - 1)
                {
                    nextLevelId = default;
                    return false;
                }

                nextLevelId = levels[i + 1].levelId;
                return true;
            }

            throw new InvalidOperationException($"Level {currentLevelId} is not advertised by the level catalog.");
        }
    }
}
