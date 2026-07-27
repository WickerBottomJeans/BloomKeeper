using System.Collections.Generic;

using System;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public interface IObjective
    {
        ObjectiveType ObjectiveType { get; }
        bool CheckObjective();
        List<ObjectiveViewData> GetViewData();
    }

    public interface IGameplayEventHandler
    {
        Type HandledEventType { get; }
        void Handle(IGameplayEvent e);
    }

    public interface IObjectiveBoardCellTargetProvider
    {
        IReadOnlyList<ObjectiveBoardCellTargetGroup> GetTargetGroups(BoardCell[,] grid);
    }
}
