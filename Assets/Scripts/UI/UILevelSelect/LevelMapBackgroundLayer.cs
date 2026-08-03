using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
        [SerializeField] private int bufferViewport = 2;
        private List<float> chunkHalfHeights = new List<float>();
        private VerticalScrollPool<RawImage> chunkPool;
        private List<Vector2> chunkPositions = new List<Vector2>();
        private readonly List<AsyncOperationHandle<Texture2D>> previewHandles = new List<AsyncOperationHandle<Texture2D>>();
        private readonly List<RawImage> previewImages = new List<RawImage>();
        private LevelMapChunkTextureCache _chunkTextureCache;
        private IReadOnlyList<ChapterBackgroundChunkData> chunks;
     
        public async UniTask ShowChapterAsync(IReadOnlyList<ChapterBackgroundChunkData> chunks)
        {
            if (chunks == null) throw new ArgumentNullException(nameof(chunks));
            if (chunks.Count == 0) throw new ArgumentException("A chapter must contain at least one background chunk.", nameof(chunks));

            DisposeChapter();
            this.chunks = chunks;
            _chunkTextureCache = new LevelMapChunkTextureCache(chunks);
            var contentHeight = CalculateChunkPositions();
            content.sizeDelta = new Vector2(0, contentHeight);
            scrollRect.verticalNormalizedPosition = 0f;

            try
            {
                await LoadPreviewsAsync();
                _chunkTextureCache.BeginInitialCapture();
                try
                {
                    chunkPool = new VerticalScrollPool<RawImage>(content, viewport, scrollRect, chunkPrefab, chunks.Count, i => chunkPositions[i], i => chunkHalfHeights[i], image => image.transform.SetSiblingIndex(previewImages.Count), (image, i) => ShowChunk(image, i), image => HideChunk(image), defaultPoolCapacity, maxPoolCapacity, bufferViewport);
                }
                finally
                {
                    _chunkTextureCache.EndInitialCapture();
                }
            }
            catch
            {
                DisposeChapter();
                throw;
            }
        }

        public UniTask WaitForInitialChunksLoaded()
        {
            if (_chunkTextureCache == null) throw new InvalidOperationException("No chapter background has been shown.");
            return _chunkTextureCache.WaitForInitialChunksLoaded();
        }

        private float CalculateChunkPositions()
        {
            chunkPositions.Clear();
            chunkHalfHeights.Clear();

            float currentY = 0f;

            for (int i = 0; i < chunks.Count; i++)
            {
                ChapterBackgroundChunkData meta = chunks[i];
                float chunkHeight = content.rect.width * (meta.height / (float)meta.width);
                float halfHeight = chunkHeight / 2f;

                chunkHalfHeights.Add(halfHeight);
                chunkPositions.Add(new Vector2(0, currentY + halfHeight));

                currentY += chunkHeight;
            }

            return currentY;
        }

        private async UniTask LoadPreviewsAsync()
        {
            foreach (ChapterBackgroundChunkData chunk in chunks)
                previewHandles.Add(Addressables.LoadAssetAsync<Texture2D>(chunk.previewAddress));

            foreach (AsyncOperationHandle<Texture2D> handle in previewHandles)
                await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            for (int index = 0; index < chunks.Count; index++)
            {
                RawImage previewImage = Instantiate(chunkPrefab, content);
                previewImage.rectTransform.localPosition = chunkPositions[index];
                previewImage.rectTransform.sizeDelta = new Vector2(content.rect.width, chunkHalfHeights[index] * 2f);
                previewImage.texture = previewHandles[index].Result;
                previewImage.transform.SetAsFirstSibling();
                previewImage.gameObject.SetActive(true);
                previewImages.Add(previewImage);
            }
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
            DisposeChapter();
        }

        private void DisposeChapter()
        {
            if (chunkPool != null)
            {
                chunkPool.Dispose();
                chunkPool = null;
            }

            if (_chunkTextureCache != null)
            {
                _chunkTextureCache.Dispose();
                _chunkTextureCache = null;
            }

            foreach (RawImage previewImage in previewImages)
                if (previewImage != null)
                    Destroy(previewImage.gameObject);
            previewImages.Clear();

            foreach (AsyncOperationHandle<Texture2D> handle in previewHandles)
                if (handle.IsValid())
                    Addressables.Release(handle);
            previewHandles.Clear();

            chunks = null;
        }
    }
}
