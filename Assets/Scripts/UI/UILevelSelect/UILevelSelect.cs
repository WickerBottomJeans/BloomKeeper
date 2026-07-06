using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UILevelSelect : MonoBehaviour
    {
        [SerializeField] private LevelMapBackgroundLayer backgroundLayer;
        [SerializeField] private LevelMapButtonLayer mapButtonLayer;

        public event Action<int> OnLevelSelected;

        private void Awake()
        {
            backgroundLayer.Init();
            mapButtonLayer.Init();
            mapButtonLayer.OnLevelSelected += HandleLevelSelected;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            mapButtonLayer.Refresh();
        }

        public UniTask WaitForInitialBackgroundLoaded()
        {
            return backgroundLayer.WaitForInitialChunksLoaded();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleLevelSelected(int levelId)
        {
            OnLevelSelected?.Invoke(levelId);
        }

        private void OnDestroy()
        {
            mapButtonLayer.OnLevelSelected -= HandleLevelSelected;
        }
    }
}
