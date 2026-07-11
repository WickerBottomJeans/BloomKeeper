using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class LevelMapBackgroundLayer : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RawImage chunkPrefab;
        [SerializeField] private int defaultPoolCapacity = 3;
        [SerializeField] private int maxPoolCapacity = 5;
        private List<float> chunkHalfHeights = new List<float>();
        private VerticalScrollPool<RawImage> chunkPool;
        private List<Vector2> chunkPositions = new List<Vector2>();
        private LevelMapChunkTextureCache _chunkTextureCache;
        private AssetManifest manifest;
    
        public void Init()
        {
            manifest = AssetManifestLoader.LoadChunkManifest();
            _chunkTextureCache = new LevelMapChunkTextureCache(manifest.assets);
            var contentHeight = CalculateChunkPositions();
            content.sizeDelta = new Vector2(0, contentHeight);

            _chunkTextureCache.BeginInitialCapture();
            try
            {
                chunkPool = new VerticalScrollPool<RawImage>(content, viewport, scrollRect, chunkPrefab, manifest.assets.Count, i => chunkPositions[i], i => chunkHalfHeights[i], image => image.transform.SetAsFirstSibling(), (image, i) => ShowChunk(image, i), image => HideChunk(image), defaultPoolCapacity, maxPoolCapacity);
            }
            finally
            {
                _chunkTextureCache.EndInitialCapture();
            }
        }

        public UniTask WaitForInitialChunksLoaded()
        {
            return _chunkTextureCache.WaitForInitialChunksLoaded();
        }

        private float CalculateChunkPositions()
        {
            chunkPositions.Clear();
            chunkHalfHeights.Clear();

            float currentY = 0f;

            for (int i = 0; i < manifest.assets.Count; i++)
            {
                AssetMetadata meta = manifest.assets[i];
                float chunkHeight = content.rect.width * (meta.height / (float)meta.width);
                float halfHeight = chunkHeight / 2f;

                chunkHalfHeights.Add(halfHeight);
                chunkPositions.Add(new Vector2(0, currentY + halfHeight));

                currentY += chunkHeight;
            }

            return currentY;
        }

        private void ShowChunk(RawImage image, int index)
        {
            float chunkHeight = chunkHalfHeights[index] * 2f;
            image.rectTransform.sizeDelta = new Vector2(content.rect.width, chunkHeight);
            _chunkTextureCache.ShowChunk(image, index);
        }

        private void HideChunk(RawImage image)
        {
            _chunkTextureCache.HideChunk(image);
        }

        private void OnDestroy()
        {
            chunkPool.Dispose();
            _chunkTextureCache.Dispose();
        }
    }
}
