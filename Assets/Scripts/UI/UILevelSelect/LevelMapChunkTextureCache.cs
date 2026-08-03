using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// owns the load/release lifecycle of level map chunk textures.
    /// </summary>
    public class LevelMapChunkTextureCache
    {
        private readonly IReadOnlyList<ChapterBackgroundChunkData> assets;
        private readonly Dictionary<int, AsyncOperationHandle<Texture2D>> chunkHandles = new Dictionary<int, AsyncOperationHandle<Texture2D>>();
        private readonly Dictionary<RawImage, int> visibleChunkIndexesByImage = new Dictionary<RawImage, int>();
        
        /// <summary>
        /// Chunks that must stay loaded even when they are not visible.
        /// </summary>
        private readonly HashSet<int> pinnedChunkIndexes = new HashSet<int>();
        private readonly HashSet<int> pendingReleaseChunkIndexes = new HashSet<int>();
        private bool isCapturingInitialChunks;

        public LevelMapChunkTextureCache(IReadOnlyList<ChapterBackgroundChunkData> assets)
        {
            this.assets = assets;
        }

        public void BeginInitialCapture()
        {
            pinnedChunkIndexes.Clear();
            pendingReleaseChunkIndexes.Clear();
            isCapturingInitialChunks = true;
        }

        public void EndInitialCapture()
        {
            isCapturingInitialChunks = false;
        }

        public void ShowChunk(RawImage image, int index)
        {
            image.texture = null;
            image.enabled = false;
            visibleChunkIndexesByImage[image] = index;

            if (isCapturingInitialChunks)
                pinnedChunkIndexes.Add(index);

            if (!chunkHandles.TryGetValue(index, out AsyncOperationHandle<Texture2D> handle) || !handle.IsValid())
            {
                handle = Addressables.LoadAssetAsync<Texture2D>(assets[index].address);
                chunkHandles[index] = handle;
            }

            if (handle.IsDone)
            {
                AssignTextureIfStillBound(image, index, handle);
                return;
            }

            handle.Completed += completedHandle => AssignTextureIfStillBound(image, index, completedHandle);
        }

        public void HideChunk(RawImage image)
        {
            int index = visibleChunkIndexesByImage[image];
            visibleChunkIndexesByImage.Remove(image);
            image.enabled = false;
            image.texture = null;
            TryReleaseUnusedChunk(index);
        }

        public async UniTask WaitForInitialChunksLoaded()
        {
            foreach (int index in new List<int>(pinnedChunkIndexes))
                await chunkHandles[index].ToUniTask();

            await UniTask.Yield();

            pinnedChunkIndexes.Clear();
            foreach (int index in new List<int>(pendingReleaseChunkIndexes))
                TryReleaseUnusedChunk(index);
        }

        public void Dispose()
        {
            foreach (AsyncOperationHandle<Texture2D> handle in chunkHandles.Values)
                if (handle.IsValid())
                    Addressables.Release(handle);

            chunkHandles.Clear();
            visibleChunkIndexesByImage.Clear();
            pinnedChunkIndexes.Clear();
            pendingReleaseChunkIndexes.Clear();
            isCapturingInitialChunks = false;
        }

        private void AssignTextureIfStillBound(RawImage image, int index, AsyncOperationHandle<Texture2D> completedHandle)
        {
            if (image == null)
                return;

            if (!visibleChunkIndexesByImage.TryGetValue(image, out int visibleIndex) || visibleIndex != index)
                return;

            if (!chunkHandles.TryGetValue(index, out AsyncOperationHandle<Texture2D> currentHandle) || !currentHandle.Equals(completedHandle))
                return;

            image.texture = completedHandle.Result;
            image.enabled = true;
        }

        private void TryReleaseUnusedChunk(int index)
        {
            if (visibleChunkIndexesByImage.ContainsValue(index))
            {
                pendingReleaseChunkIndexes.Remove(index);
                return;
            }

            if (pinnedChunkIndexes.Contains(index))
            {
                pendingReleaseChunkIndexes.Add(index);
                return;
            }

            pendingReleaseChunkIndexes.Remove(index);
            if (!chunkHandles.TryGetValue(index, out AsyncOperationHandle<Texture2D> handle))
                return;

            if (handle.IsValid())
                Addressables.Release(handle);
            chunkHandles.Remove(index);
        }
    }
}
