using System.Collections.Generic;
using Newtonsoft.Json;

namespace DefaultNamespace
{

    
    public class PetalGoal
    {
        public PetalType petalType;
        public int amount;
    }

    //Fat, bloated 
    public class ObjectiveData
    {
        public ObjectiveType type;
        
        //Only use for type Match
        public List<PetalGoal> petals;
    }
}