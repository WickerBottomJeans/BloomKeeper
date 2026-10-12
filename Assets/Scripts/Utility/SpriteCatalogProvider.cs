using Cysharp.Threading.Tasks;
using DefaultNamespace.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DefaultNamespace
{
    public sealed class SpriteCatalogProvider : Singleton<SpriteCatalogProvider>
    {
        [SerializeField] private AssetReference petalSpriteCatalogReference;
        [SerializeField] private AssetReference objectiveSpriteCatalogReference;
        private PetalSpriteCatalog petalSpriteCatalog;
        private ObjectiveSpriteCatalog objectiveSpriteCatalog;

        public async UniTask LoadAsync()
        {
            petalSpriteCatalog = await petalSpriteCatalogReference.LoadAssetAsync<PetalSpriteCatalog>().ToUniTask();
            objectiveSpriteCatalog = await objectiveSpriteCatalogReference.LoadAssetAsync<ObjectiveSpriteCatalog>().ToUniTask();
        }

        public Sprite GetPetalSprite(PetalType petalType, SpecialSkillType skillType) => petalSpriteCatalog.GetSprite(petalType, skillType);
        public Sprite GetObjectiveSprite(ObjectiveType objectiveType, PetalType petalType) => objectiveSpriteCatalog.GetSprite(objectiveType, petalType);
    }
}
