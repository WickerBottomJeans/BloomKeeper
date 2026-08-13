using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    [CreateAssetMenu(fileName = "BoosterTargetPresentationConfig", menuName = "BloomKeeper/Boosters/Booster Target Presentation Config")]
    public class BoosterTargetPresentationConfig : ScriptableObject
    {
        [SerializeField] private List<BoosterTargetMaterialMapping> materialMappings = new List<BoosterTargetMaterialMapping>();

        public BoosterTargetMaterialMapping GetPresentation(BoosterType boosterType)
        {
            foreach (BoosterTargetMaterialMapping materialMapping in materialMappings)
                if (materialMapping.BoosterType == boosterType)
                    return materialMapping;

            throw new InvalidOperationException($"BoosterTargetPresentationConfig has no material for booster type: {boosterType}.");
        }

        [Serializable]
        public class BoosterTargetMaterialMapping
        {
            [SerializeField] private BoosterType boosterType;
            [SerializeField] private Material material;

            public BoosterType BoosterType => boosterType;
            public Material Material => material;
        }
    }
}
