namespace DefaultNamespace
{
    //Fat, bloated 
    [Serializable]
    public class PetalGoal
    {
        public string color;
        public int amount;
    }

    [Serializable]
    public class ObjectiveData
    {
        public string type;
        public List<PetalGoal> petals;
    }
}