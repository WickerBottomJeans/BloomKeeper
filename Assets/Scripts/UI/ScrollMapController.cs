using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ScrollMapController : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private float backgroundAspectRatio;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;

        [Header("Pooling Settings")]
        [SerializeField] private int defaultPoolCapacity = 10;
        [SerializeField] private int maxPoolSize = 50;

        private VerticalScrollPool<LevelButton> scrollPool;
        private List<LevelMeta> allMetas;
        private float halfHeightBtn;

        private void Start()
        {
            SetContentSize();
            LoadMetas();
            
            halfHeightBtn = levelButtonPrefab.GetComponent<RectTransform>().rect.height / 2f;

            scrollPool = new VerticalScrollPool<LevelButton>(
                content,
                viewport,
                scrollRect,
                levelButtonPrefab,
                allMetas.Count,
                i => new Vector2(
                    allMetas[i].normalizedX * content.rect.width,
                    allMetas[i].normalizedY * content.rect.height
                ), i => this.halfHeightBtn,
                null,
                (button, i) => button.Init(allMetas[i]),
                button => { },
                defaultPoolCapacity,
                maxPoolSize
            );
        }

        
        //TODO: we  need a more dynamic way to set this other than manually set the BGAspectRatio
        private void SetContentSize()
        {
            float contentHeight = Screen.width * backgroundAspectRatio;
            content.sizeDelta = new Vector2(0, contentHeight);
        }

        private void LoadMetas()
        {
            allMetas = LevelLoader.LoadMetas();
        }

        private void OnDestroy()
        {
            scrollPool.Dispose();
        }
    }
}