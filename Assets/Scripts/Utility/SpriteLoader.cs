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
            foreach (string key in atlasKeys)
            {
                if (atlases.ContainsKey(key)) continue;
                SpriteAtlas atlas = await Addressables.LoadAssetAsync<SpriteAtlas>(key).ToUniTask();
                atlases[key] = atlas;
            }
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