using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BoosterButtonConfig", menuName = "BloomKeeper/UI/Booster Button Config")]
public class BoosterButtonConfig : ScriptableObject
{
    [FormerlySerializedAs("iconMappings")]
    [SerializeField] private List<BoosterButtonMapping> buttonMappings = new List<BoosterButtonMapping>();

    public Sprite GetIcon(BoosterType boosterType) => GetMapping(boosterType).Icon;

    public string GetGuidanceText(BoosterType boosterType)
    {
        string guidanceText = GetMapping(boosterType).GuidanceText;
        if (string.IsNullOrWhiteSpace(guidanceText))
            throw new InvalidOperationException($"BoosterButtonConfig has no guidance text for booster type: {boosterType}.");

        return guidanceText;
    }

    private BoosterButtonMapping GetMapping(BoosterType boosterType)
    {
        foreach (BoosterButtonMapping buttonMapping in buttonMappings)
            if (buttonMapping.BoosterType == boosterType)
                return buttonMapping;

        throw new InvalidOperationException($"BoosterButtonConfig has no mapping for booster type: {boosterType}.");
    }

    [Serializable]
    public class BoosterButtonMapping
    {
        [SerializeField] private BoosterType boosterType;
        [SerializeField] private Sprite icon;
        [SerializeField] private string guidanceText;

        public BoosterType BoosterType => boosterType;
        public Sprite Icon => icon;
        public string GuidanceText => guidanceText;
    }
}
