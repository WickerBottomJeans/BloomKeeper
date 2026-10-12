using System;
using System.Collections.Generic;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public sealed class CollectEggObjective : IObjective
    {
        private int remainingEggs;
        public ObjectiveType ObjectiveType => ObjectiveType.CollectEgg;

        public CollectEggObjective(ObjectiveJson json) => remainingEggs = json.eggsToCollect;
        public bool CheckObjective() => remainingEggs == 0;
        public void Apply(BoardResolution resolution) => remainingEggs = Math.Max(0, remainingEggs - resolution.EggCollections.Count);
        public List<ObjectiveViewData> GetViewData() => new() { new ObjectiveViewData { objectiveType = ObjectiveType, objectiveText = remainingEggs.ToString(), remainingAmount = remainingEggs } };
    }
}
