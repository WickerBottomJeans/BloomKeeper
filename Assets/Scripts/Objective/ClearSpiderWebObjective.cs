using System;
using System.Collections.Generic;
using DefaultNamespace.Utility;

namespace DefaultNamespace
{
    public class ClearSpiderWebObjective : IObjective, IGameplayEventHandler
    {
        private int spiderWebCount;

        public ClearSpiderWebObjective(ObjectiveJson json)
        {
            spiderWebCount = json.spiderWebsToClear;
        }

        public ObjectiveType ObjectiveType { get; } = ObjectiveType.ClearSpiderWeb;
        public bool CheckObjective() => spiderWebCount <= 0;
        public Type HandledEventType => typeof(SpiderWebClearedEvent);

        public void Handle(IGameplayEvent e)
        {
            SpiderWebClearedEvent cleared = (SpiderWebClearedEvent)e;
            if (cleared.CleanedTileCount > spiderWebCount)
                throw new InvalidOperationException("Cleared spider web tile count exceeds remaining spider web objective count.");
            spiderWebCount -= cleared.CleanedTileCount;
        }

        public List<ObjectiveViewData> GetViewData()
        {
            return new List<ObjectiveViewData>
            {
                new ObjectiveViewData
                {
                    spriteKey = SpriteKeyHelper.GetObjectiveSpriteKey(ObjectiveType),
                    objectiveText = spiderWebCount.ToString(),
                    remainingAmount = spiderWebCount
                }
            };
        }
    }
}
