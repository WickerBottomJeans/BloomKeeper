using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace DefaultNamespace
{
    public class SpriteLoader : Singleton<SpriteLoader>
    {
        [SerializeField] private List<string> atlasKeys;
        private readonly Dictionary<string, SpriteAtlas> atlases = new();

        public async UniTask LoadAll()
        {
            List<string> pendingKeys = new();
            List<UniTask<SpriteAtlas>> pendingLoads = new();

            foreach (string key in atlasKeys)
            {
                if (atlases.ContainsKey(key)) continue;
                pendingKeys.Add(key);
                pendingLoads.Add(Addressables.LoadAssetAsync<SpriteAtlas>(key).ToUniTask());
            }

            SpriteAtlas[] loadedAtlases = await UniTask.WhenAll(pendingLoads);
            for (int index = 0; index < pendingKeys.Count; index++) atlases[pendingKeys[index]] = loadedAtlases[index];
        }

        public Sprite GetSprite(string spriteKey)
        {
            foreach (SpriteAtlas atlas in atlases.Values)
            {
                Sprite sprite = atlas.GetSprite(spriteKey);
                if (sprite != null) return sprite;
            }
            Debug.LogWarning($"Sprite not found: {spriteKey}");
            return null;
        }
    }
}
