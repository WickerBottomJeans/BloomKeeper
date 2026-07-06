using System.Threading.Tasks;
using DefaultNamespace.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DefaultNamespace.UI
{
    public partial class UIManager : Singleton<UIManager>
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform uiRoot;
        
        private async Task<T> LoadPanel<T>(string address) where T : Component
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;
            GameObject instance = Instantiate(handle.Result, uiRoot);
            return instance.GetComponent<T>();
        }
    }
}
