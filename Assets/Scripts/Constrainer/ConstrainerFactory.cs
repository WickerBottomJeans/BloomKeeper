using System;

namespace DefaultNamespace
{
    public static class ConstrainerFactory
    {
        public static IConstrainer Create(ConstrainerJson json)
        {
            switch (json.type)
            {
                case ConstrainerType.MoveLimit: return new MoveConstrainer(json);
                case ConstrainerType.TimeLimit: return new TimerConstrainer(json);
                default: throw new Exception($"Unknown constrainer type: {json.type}");
            }
        }
    }
}
