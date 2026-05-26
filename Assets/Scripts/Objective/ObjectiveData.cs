using System.Collections.Generic;
using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class PetalGoal
    {
        public PetalType petalType;
        public int amount;
    }

    //fat DTO - only for JSON deserialization
    public class ObjectiveData
    {
        public ObjectiveType type;
        
        //Only use for type Match
        public List<PetalGoal> petals;
    }
}