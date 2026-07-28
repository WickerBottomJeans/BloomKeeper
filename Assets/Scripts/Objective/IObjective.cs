using System.Collections.Generic;

using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public interface IObjective
    {
        ObjectiveType ObjectiveType { get; }
        bool CheckObjective();
        void Apply(IReadOnlyList<TileChange> changes);
        List<ObjectiveViewData> GetViewData();
    }

    public interface IObjectiveTileTargetProvider
    {
        IReadOnlyList<ObjectiveTileTargetGroup> GetTargetGroups(IReadOnlyList<TileState> boardSnapshot);
    }
}
