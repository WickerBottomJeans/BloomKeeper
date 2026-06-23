using System.Collections.Generic;
using Newtonsoft.Json;

namespace DefaultNamespace
{
    public class PetalGoal
    {
        public PetalType petalType;
        public int amount;
    }

    /// <summary>
    /// Fat :)
    /// </summary>
    public class ObjectiveJson
    {
        public ObjectiveType type;
        
        //Only use for type Match
        public List<PetalGoal> petals;

        //Only use for type ClearSpiderWeb
        public int spiderWebsToClear;
    }
}
