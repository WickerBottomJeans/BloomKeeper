using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UILevelSelect : MonoBehaviour
    {
        [SerializeField] private LevelMapBackgroundLayer backgroundLayer;
        [SerializeField] private LevelMapButtonLayer mapButtonLayer;

        private bool isMapButtonLayerInitialized;

        public event Action<int> OnLevelSelected;

        private void Awake()
        {
            backgroundLayer.Init();
            mapButtonLayer.OnLevelSelected += HandleLevelSelected;
        }

        public void Show(PlayerProgressionData progression)
        {
            gameObject.SetActive(true);
            if (!isMapButtonLayerInitialized)
            {
                mapButtonLayer.Init(progression);
                isMapButtonLayerInitialized = true;
                return;
            }

            mapButtonLayer.Refresh(progression);
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
