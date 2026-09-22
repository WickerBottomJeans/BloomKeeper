using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILoading loadingPrefab;

        private UILoading loadingInstance;
        private int loadingVisibilityVersion;
        private bool isLoadingCapturePending;
        private UniTask loadingCaptureTask;

        public async UniTask ShowLoading(string text = "Loading . . .")
        {
            loadingVisibilityVersion++;
            UILoading loading = loadingInstance != null && loadingInstance.gameObject.activeSelf ? loadingInstance : GetOrCreateLoading();
            if (!loading.gameObject.activeSelf)
            {
                if (!isLoadingCapturePending)
                {
                    isLoadingCapturePending = true;
                    loadingCaptureTask = CaptureLoadingBackground(loading).Preserve();
                }

                await loadingCaptureTask;
                loading.gameObject.SetActive(true);
                isLoadingCapturePending = false;
            }
            loading.SetText(text);
            await loading.ShowLoadingBackground();
            await UniTask.NextFrame(cancellationToken: destroyCancellationToken);
        }

        public void SetLoadingText(string text)
        {
            GetOrCreateLoading().SetText(text);
        }

        private async UniTask CaptureLoadingBackground(UILoading loading)
        {
            await UniTask.WaitForEndOfFrame(this, destroyCancellationToken);
            loading.CaptureLoadingBackground();
        }

        public async UniTask HideLoading()
        {
            if (loadingInstance == null) return;
            int hideVisibilityVersion = ++loadingVisibilityVersion;
            if (!loadingInstance.gameObject.activeSelf)
            {
                isLoadingCapturePending = false;
                loadingInstance.ReleaseLoadingBackground();
                return;
            }
            await loadingInstance.HideLoadingBackground();
            if (hideVisibilityVersion == loadingVisibilityVersion)
                loadingInstance.gameObject.SetActive(false);
        }

        private UILoading GetOrCreateLoading()
        {
            bool isNewLoadingInstance = loadingInstance == null;
            UILoading loading = GetPanel(ref loadingInstance, loadingPrefab, overlayRoot);
            if (isNewLoadingInstance) loading.gameObject.SetActive(false);
            return loading;
        }
    }
}
