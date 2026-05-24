using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class VerticalScrollPool<T> where T : Component
    {
        private readonly RectTransform content;
        private readonly RectTransform viewport;
        private readonly ScrollRect scrollRect;
        private readonly Func<int, Vector2> getPosition;
        private readonly Action<T> onCreate;
        private readonly Action<T, int> onShow;
        private readonly Action<T> onHide;
        private readonly Func<int, float> getHalfHeight;
        private readonly IObjectPool<T> pool;
        private readonly Dictionary<int, T> visibleItems = new Dictionary<int, T>();
        private readonly float viewportHeight;
        
        /// <summary>
        /// Number of extra viewports to pre-load.
        /// </summary>
        private readonly int bufferViewport;

        private int itemCount;

        public VerticalScrollPool(
            RectTransform content,
            RectTransform viewport,
            ScrollRect scrollRect,
            GameObject prefab,
            int itemCount,
            Func<int, Vector2> getPosition,
            Func<int, float> getHalfHeight,
            Action<T> onCreate,
            Action<T, int> onShow,
            Action<T> onHide,
            int defaultCapacity = 10,
            int maxSize = 50,
            int bufferViewport = 1)
        {
            this.content = content;
            this.viewport = viewport;
            this.scrollRect = scrollRect;
            this.getPosition = getPosition;
            this.getHalfHeight = getHalfHeight;
            this.onShow = onShow;
            this.onHide = onHide;
            this.itemCount = itemCount;
            this.viewportHeight = viewport.rect.height;
            this.bufferViewport = bufferViewport;
            pool = new ObjectPool<T>(
                () =>
                {
                    GameObject go = GameObject.Instantiate(prefab);
                    go.SetActive(false);
                    go.transform.SetParent(content, false);
                    T item = go.GetComponent<T>();
                    onCreate?.Invoke(item);
                    return item;
                },
                item => item.gameObject.SetActive(true),
                item => item.gameObject.SetActive(false),
                item => GameObject.Destroy(item.gameObject),
                true, defaultCapacity, maxSize
            );
            scrollRect.onValueChanged.AddListener(OnScroll);
            OnScroll(scrollRect.normalizedPosition);
        }

        private void OnScroll(Vector2 scrollPos)
        {
            float scrolledY = scrollPos.y * (content.rect.height - viewportHeight);
            HashSet<int> shouldBeVisible = new HashSet<int>();

            for (int i = 0; i < itemCount; i++)
            {
                float itemY = getPosition(i).y;
                float halfHeight = getHalfHeight(i);
                float buffer = viewportHeight * bufferViewport;

                bool isVisible = (itemY + halfHeight) >= scrolledY - buffer &&
                                 (itemY - halfHeight) <= scrolledY + viewportHeight + buffer;
                if (isVisible) shouldBeVisible.Add(i);
            }

            foreach (int i in visibleItems.Keys.ToList())
            {
                if (!shouldBeVisible.Contains(i))
                {
                    onHide(visibleItems[i]);
                    pool.Release(visibleItems[i]);
                    visibleItems.Remove(i);
                }
            }

            foreach (int i in shouldBeVisible)
            {
                if (!visibleItems.ContainsKey(i))
                {
                    T item = pool.Get();
                    item.GetComponent<RectTransform>().localPosition = getPosition(i);
                    onShow(item, i);
                    visibleItems[i] = item;
                }
            }
        }

        public void Refresh()
        {
            foreach (var kvp in visibleItems)
                onShow(kvp.Value, kvp.Key);
    
            OnScroll(scrollRect.normalizedPosition);
        }

        public void Dispose()
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }
}