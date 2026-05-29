using System;
using DefaultNamespace;
using UnityEngine;

/// <summary>
/// Will valotize as we add more objective, could've used dict map or reflection but the game doenst have that much objective anyway
/// </summary>
public static class ObjectiveFactory
{
    
    public static IObjective Create(ObjectiveJson json)
    {
        switch (json.type)
        {
            case ObjectiveType.Match: return new MatchObjective(json);
            //case ObjectiveType.Butterfly: return new ButterflyObjective(data);
            default: throw new Exception($"Unknown objective type: {json.type}");
        }
    }
}
