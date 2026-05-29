using System.Collections.Generic;

namespace DefaultNamespace
{
    public abstract class ObjectiveDTO { }

    public class PetalsClearedEvent : ObjectiveDTO
    {
        public List<PetalType> ClearedPetals { get; }

        public PetalsClearedEvent(List<PetalType> clearedPetals)
        {
            ClearedPetals = clearedPetals;
        }
    }
}