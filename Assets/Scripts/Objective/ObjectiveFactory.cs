using System;
using DefaultNamespace;
using UnityEngine;

/// <summary>
/// Will valotize as we add more objective, could've used dict map or reflection but the game doenst have that much objective anyway
/// </summary>
public static class ObjectiveFactory
{
    
    public static IObjective Create(ObjectiveData data)
    {
        switch (data.type)
        {
            case ObjectiveType.Match: return new MatchObjective(data);
            //case ObjectiveType.Butterfly: return new ButterflyObjective(data);
            default: throw new Exception($"Unknown objective type: {data.type}");
        }
    }
}
