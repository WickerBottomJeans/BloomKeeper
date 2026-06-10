using System.Collections.Generic;
using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class PetalGoal
    {
        public PetalType petalType;
        public int amount;
        public string spriteKey;
    }

    public class ObjectiveJson
    {
        public ObjectiveType type;
        public string spriteKey;
        
        //Only use for type Match
        public List<PetalGoal> petals;
    }
}