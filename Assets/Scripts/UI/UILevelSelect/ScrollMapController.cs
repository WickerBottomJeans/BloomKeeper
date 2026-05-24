using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ScrollMapController : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;

        [Header("Pooling Settings")]
        [SerializeField] private int defaultPoolCapacity = 10;
        [SerializeField] private int maxPoolSize = 50;

        private VerticalScrollPool<LevelButton> scrollPool;
        private LevelMetaCollection metaCollection;
        private List<LevelMeta> allMetas;
        private float halfHeightBtn;

        public void Init()
        {
            LoadMetas();
            
            halfHeightBtn = levelButtonPrefab.GetComponent<RectTransform>().rect.height / 2f;

            scrollPool = new VerticalScrollPool<LevelButton>(
                content,
                viewport,
                scrollRect,
                levelButtonPrefab,
                allMetas.Count,
                i => new Vector2(
                    allMetas[i].pixelX * (content.rect.width / metaCollection.referenceScreenWidth),
                    allMetas[i].pixelY * (content.rect.width / metaCollection.referenceScreenWidth)
                ), i => this.halfHeightBtn,
                null,
                (button, i) => button.Init(allMetas[i]),
                button => { },
                defaultPoolCapacity,
                maxPoolSize
            );
        }
        

        private void LoadMetas()
        {
            metaCollection = LevelLoader.LoadMetas();
            allMetas = metaCollection.levels;
        }

        private void OnDestroy()
        {
            scrollPool.Dispose();
        }
        
        public void Refresh()
        {
            scrollPool.Refresh();
        }
    }
}