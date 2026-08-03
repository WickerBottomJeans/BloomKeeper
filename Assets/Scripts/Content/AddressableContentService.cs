using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DefaultNamespace
{
    public class AddressableContentService
    {
        public async UniTask InitializeAsync()
        {
            AsyncOperationHandle initializationHandle = Addressables.InitializeAsync(false);
            try
            {
                await initializationHandle.ToUniTask();
            }
            finally
            {
                Addressables.Release(initializationHandle);
            }
        }

        public async UniTask<long> GetDownloadSizeAsync(string label)
        {
            ValidateLabel(label);
            AsyncOperationHandle<long> sizeHandle = Addressables.GetDownloadSizeAsync(label);
            try
            {
                return await sizeHandle.ToUniTask();
            }
            finally
            {
                Addressables.Release(sizeHandle);
            }
        }

        public async UniTask EnsureDownloadedAsync(string label)
        {
            ValidateLabel(label);
            AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(label, false);
            try
            {
                await downloadHandle.ToUniTask();
            }
            finally
            {
                Addressables.Release(downloadHandle);
            }
        }

        private static void ValidateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Addressables label must contain a value.", nameof(label));
        }
    }
}
