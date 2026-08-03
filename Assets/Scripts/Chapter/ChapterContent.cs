using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public class ChapterContent
    {
        private readonly IReadOnlyDictionary<int, LevelData> levelDefinitions;

        public ChapterDefinition Definition { get; }

        public ChapterContent(ChapterDefinition definition, IReadOnlyDictionary<int, LevelData> levelDefinitions)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.levelDefinitions = levelDefinitions ?? throw new ArgumentNullException(nameof(levelDefinitions));
        }

        public LevelData GetLevelDefinition(int levelId)
        {
            if (!levelDefinitions.TryGetValue(levelId, out LevelData levelDefinition))
                throw new KeyNotFoundException($"Chapter {Definition.chapterId} has no loaded definition for referenced level {levelId}.");
            return levelDefinition;
        }
    }
}
