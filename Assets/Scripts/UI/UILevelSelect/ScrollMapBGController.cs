using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ScrollMapBGController : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private int defaultPoolCapacity = 3;
    [SerializeField] private int maxPoolCapacity = 5;
    private List<float> chunkHalfHeights = new List<float>();
    private VerticalScrollPool<RawImage> chunkPool;
    private List<Vector2> chunkPositions = new List<Vector2>();
    /// <summary>
    /// Choose this instead of sprite sheet to manage VRAM better, those image are huge so
    /// </summary>
    private Dictionary<int, AsyncOperationHandle<Texture2D>> chunkHandles = new Dictionary<int, AsyncOperationHandle<Texture2D>>();
    private AssetManifest manifest;
    
    public void Init()
    {
        manifest = AssetManifestLoader.LoadChunkManifest();
        var contentHeight = CalculateChunkPositions();
        content.sizeDelta = new Vector2(0, contentHeight);

        chunkPool = new VerticalScrollPool<RawImage>(
            content, viewport, scrollRect, chunkPrefab,
            manifest.assets.Count,
            i => chunkPositions[i],
            i => chunkHalfHeights[i],
            image => image.transform.SetAsFirstSibling(),
            (image, i) => LoadChunk(image, i),
            image => ReleaseChunk(image),
            defaultPoolCapacity, maxPoolCapacity
        );
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

    private void LoadChunk(RawImage image, int index)
    {
        float chunkHeight = chunkHalfHeights[index] * 2f;
        image.rectTransform.sizeDelta = new Vector2(content.rect.width, chunkHeight);

        string address = manifest.assets[index].address;
        var handle = Addressables.LoadAssetAsync<Texture2D>(address);
        chunkHandles[index] = handle;
        handle.Completed += h =>
        {
            if (image != null)
                image.texture = h.Result;
        };
    }

    private void ReleaseChunk(RawImage image)
    {
        foreach (var kvp in chunkHandles)
        {
            if (kvp.Value.IsValid() && kvp.Value.Result == image.texture)
            {
                image.texture = null;
                Addressables.Release(kvp.Value);
                chunkHandles.Remove(kvp.Key);
                break;
            }
        }
    }

    private void OnDestroy()
    {
        chunkPool.Dispose();
        foreach (var handle in chunkHandles.Values)
            if (handle.IsValid())
                Addressables.Release(handle);
    }
    
    
}
}