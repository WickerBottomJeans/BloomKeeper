using System.Collections.Generic;

using System;

namespace DefaultNamespace
{
    public interface IObjective
    {
        ObjectiveType ObjectiveType { get; }
        bool CheckObjective();
        List<ObjectiveViewData> GetViewData();
    }

    public interface IObjectiveEventHandler
    {
        Type HandledEventType { get; }
        void Handle(IObjectiveEvent e);
    }
}
