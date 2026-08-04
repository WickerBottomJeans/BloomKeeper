using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class HorizontalScrollPool<T> : IDisposable where T : Component
    {
        private readonly RectTransform content;
        private readonly ScrollRect scrollRect;
        private readonly Func<int, Vector2> getPosition;
        private readonly Func<int, float> getHalfWidth;
        private readonly Func<int, Transform> getParent;
        private readonly Action<T, int> onShow;
        private readonly Action<T> onHide;
        private readonly ObjectPool<T> pool;
        private readonly Dictionary<int, T> visibleItems = new();
        private readonly float viewportWidth;
        private readonly int bufferViewport;
        private readonly int itemCount;

        public HorizontalScrollPool(RectTransform content, RectTransform viewport, ScrollRect scrollRect, T prefab, int itemCount, Func<int, Vector2> getPosition, Func<int, float> getHalfWidth, Func<int, Transform> getParent, Action<T> onCreate, Action<T, int> onShow, Action<T> onHide, int defaultCapacity = 3, int maxSize = 5, int bufferViewport = 1)
        {
            this.content = content;
            this.scrollRect = scrollRect;
            this.getPosition = getPosition;
            this.getHalfWidth = getHalfWidth;
            this.getParent = getParent;
            this.onShow = onShow;
            this.onHide = onHide;
            this.itemCount = itemCount;
            viewportWidth = viewport.rect.width;
            this.bufferViewport = bufferViewport;
            pool = new ObjectPool<T>(() =>
            {
                T item = UnityEngine.Object.Instantiate(prefab, content);
                item.gameObject.SetActive(false);
                onCreate?.Invoke(item);
                return item;
            }, item => item.gameObject.SetActive(true), item => item.gameObject.SetActive(false), item => UnityEngine.Object.Destroy(item.gameObject), true, defaultCapacity, maxSize);

            scrollRect.onValueChanged.AddListener(OnScroll);
            OnScroll(scrollRect.normalizedPosition);
        }

        public void Dispose()
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
            foreach (T item in visibleItems.Values)
            {
                onHide(item);
                pool.Release(item);
            }
            visibleItems.Clear();
            pool.Clear();
        }

        private void OnScroll(Vector2 scrollPosition)
        {
            float scrolledX = scrollPosition.x * (content.rect.width - viewportWidth);
            float buffer = viewportWidth * bufferViewport;
            var shouldBeVisible = new HashSet<int>();

            for (int index = 0; index < itemCount; index++)
            {
                float itemX = getPosition(index).x;
                float halfWidth = getHalfWidth(index);
                bool isVisible = itemX + halfWidth >= scrolledX - buffer && itemX - halfWidth <= scrolledX + viewportWidth + buffer;
                if (isVisible) shouldBeVisible.Add(index);
            }

            foreach (int index in visibleItems.Keys.Where(index => !shouldBeVisible.Contains(index)).ToArray())
            {
                T item = visibleItems[index];
                onHide(item);
                pool.Release(item);
                visibleItems.Remove(index);
            }

            foreach (int index in shouldBeVisible)
            {
                if (visibleItems.ContainsKey(index)) continue;
                T item = pool.Get();
                item.transform.SetParent(getParent(index), false);
                onShow(item, index);
                visibleItems.Add(index, item);
            }
        }
    }
}
