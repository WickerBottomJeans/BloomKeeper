using System;
using DefaultNamespace;
using UnityEngine;

/// <summary>
/// Will valotize as we add more objective, could've used dict map or reflection but the game doenst have that much objective anyway
/// </summary>
public static class ObjectiveFactory
{
    //TODO: make sure to check if the map even have enough spider web for   ObjectiveType.ClearSpiderWeb
    public static IObjective Create(ObjectiveJson json)
    {
        switch (json.type)
        {
            case ObjectiveType.Match: return new MatchObjective(json);
            case ObjectiveType.ClearSpiderWeb: return new ClearSpiderWebObjective(json);
            //case ObjectiveType.Butterfly: return new ButterflyObjective(data);
            default: throw new Exception($"Unknown objective type: {json.type}");
        }
    }
}
