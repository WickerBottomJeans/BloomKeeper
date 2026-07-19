using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UILoading loadingPrefab;

        private UILoading loadingInstance;

        public void ShowLoading(string text = "Loading . . .")
        {
            UILoading loading = GetOrCreateLoading();
            loading.SetText(text);
            loading.gameObject.SetActive(true);
        }

        public void SetLoadingText(string text)
        {
            GetOrCreateLoading().SetText(text);
        }

        public void HideLoading()
        {
            loadingInstance?.gameObject.SetActive(false);
        }

        private UILoading GetOrCreateLoading()
        {
            if (loadingInstance == null)
                loadingInstance = Instantiate(loadingPrefab, overlayRoot);

            return loadingInstance;
        }
    }
}
