using System.Collections.Generic;

namespace DefaultNamespace
{
    public interface IObjective
    {
        ObjectiveType ObjectiveType { get; }
        bool CheckObjective();
        void Report(ObjectiveDTO e);
        List<ObjectiveViewData> GetViewData();
    }
}