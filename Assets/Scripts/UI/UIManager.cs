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
        [SerializeField] private RectTransform overlayRoot;

        private T GetPanel<T>(ref T panelInstance, T panelPrefab, Transform panelParent) where T : Component
        {
            if (panelInstance == null) panelInstance = Instantiate(panelPrefab, panelParent);
            panelInstance.transform.SetAsLastSibling();
            return panelInstance;
        }
    }
}
