using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private Button backdropPrefab;
        private Button backdropInstance;
        
        public void ShowBackdrop(Action onDismiss, Transform popupRoot)
        {
            if (backdropInstance == null)
                backdropInstance = Instantiate(backdropPrefab, uiRoot);

            backdropInstance.onClick.RemoveAllListeners();
            backdropInstance.onClick.AddListener(() => onDismiss?.Invoke());
            backdropInstance.transform.SetSiblingIndex(popupRoot.GetSiblingIndex() - 1);
            backdropInstance.gameObject.SetActive(true);
        }

        public void HideBackdrop()
        {
            if (backdropInstance == null) return;
            backdropInstance.onClick.RemoveAllListeners();
            backdropInstance.gameObject.SetActive(false);
        }
    }
}
