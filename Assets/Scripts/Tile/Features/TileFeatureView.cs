using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace
{
    public abstract class TileFeatureView : MonoBehaviour
    {
        public abstract TileFeatureType FeatureType { get; }
        public abstract void InitializeFeatureView(float tileSize);
        public abstract void DisplayFeatureState(TileFeatureState tileFeatureState);
        public abstract UniTask PlayFeatureSpawn();
        public abstract UniTask PlayFeatureChange(TileFeatureState tileFeatureState);
        public abstract UniTask PlayFeatureRemoval();
    }
}
