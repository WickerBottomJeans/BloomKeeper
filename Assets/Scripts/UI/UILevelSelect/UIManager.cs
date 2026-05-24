using System.Threading.Tasks;
using DefaultNamespace.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DefaultNamespace.UI
{
    public partial class UIManager : Singleton<UIManager>
    {
        [SerializeField] private Canvas canvas;
        
        private async Task<T> LoadPanel<T>(string address) where T : Component
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;
            GameObject instance = Instantiate(handle.Result, canvas.transform);
            return instance.GetComponent<T>();
        }
    }
}