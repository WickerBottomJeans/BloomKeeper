using System.Collections.Generic;
using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class PetalGoal
    {
        public PetalType petalType;
        public int amount;
    }

    //fat - only for JSON deserialization
    public class ObjectiveJson
    {
        public ObjectiveType type;
        
        //Only use for type Match
        public List<PetalGoal> petals;
    }
}