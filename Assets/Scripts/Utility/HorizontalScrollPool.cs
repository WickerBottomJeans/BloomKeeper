using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class HorizontalScrollPool<T> : IDisposable where T : Component
    {
        private readonly RectTransform content;
        private readonly RectTransform viewport;
        private readonly ScrollRect scrollRect;
        private readonly IScrollPoolGeometrySource geometrySource;
        private readonly Func<int, Transform> getParent;
        private readonly Action<T, int> onShow;
        private readonly Action<T> onHide;
        private readonly ObjectPool<T> pool;
        private readonly Dictionary<int, T> visibleItems = new();
        private readonly int[] orderedIndices;
        private readonly int bufferViewport;
        private int visibleStartRank;
        private int visibleEndRankExclusive;

        public HorizontalScrollPool(RectTransform content, RectTransform viewport, ScrollRect scrollRect, T prefab, IScrollPoolGeometrySource geometrySource, Func<int, Transform> getParent, Action<T> onCreate, Action<T, int> onShow, Action<T> onHide, int defaultCapacity = 3, int maxSize = 5, int bufferViewport = 1)
        {
            this.content = content;
            this.viewport = viewport;
            this.scrollRect = scrollRect;
            this.geometrySource = geometrySource;
            this.getParent = getParent;
            this.onShow = onShow;
            this.onHide = onHide;
            this.bufferViewport = bufferViewport;
            orderedIndices = BuildOrderedIndices();
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

        private int[] BuildOrderedIndices()
        {
            int[] indices = new int[geometrySource.Count];
            for (int index = 0; index < indices.Length; index++) indices[index] = index;
            Array.Sort(indices, CompareItemStarts);

            for (int rank = 1; rank < indices.Length; rank++)
            {
                if (GetItemEnd(indices[rank - 1]) > GetItemEnd(indices[rank])) throw new ArgumentException("Scroll pool item end edges must be nondecreasing after sorting by start edge.", nameof(geometrySource));
            }

            return indices;
        }

        private int CompareItemStarts(int leftIndex, int rightIndex)
        {
            int startComparison = GetItemStart(leftIndex).CompareTo(GetItemStart(rightIndex));
            if (startComparison != 0) return startComparison;
            int endComparison = GetItemEnd(leftIndex).CompareTo(GetItemEnd(rightIndex));
            return endComparison != 0 ? endComparison : leftIndex.CompareTo(rightIndex);
        }

        private float GetItemStart(int index)
        {
            ScrollPoolItemGeometry geometry = geometrySource.GetGeometry(index);
            return geometry.Position.x - geometry.HalfExtent;
        }

        private float GetItemEnd(int index)
        {
            ScrollPoolItemGeometry geometry = geometrySource.GetGeometry(index);
            return geometry.Position.x + geometry.HalfExtent;
        }

        private void OnScroll(Vector2 scrollPosition)
        {
            float viewportWidth = viewport.rect.width;
            float scrolledX = scrollPosition.x * (content.rect.width - viewportWidth);
            float buffer = viewportWidth * bufferViewport;
            int newStartRank = FindFirstRankWithEndAtOrAfter(scrolledX - buffer);
            int newEndRankExclusive = FindFirstRankWithStartAfter(scrolledX + viewportWidth + buffer);
            UpdateVisibleRange(newStartRank, newEndRankExclusive);
        }

        private int FindFirstRankWithEndAtOrAfter(float viewportStart)
        {
            int low = 0;
            int high = orderedIndices.Length;
            while (low < high)
            {
                int middleRank = low + (high - low) / 2;
                if (GetItemEnd(orderedIndices[middleRank]) < viewportStart) low = middleRank + 1;
                else high = middleRank;
            }
            return low;
        }

        private int FindFirstRankWithStartAfter(float viewportEnd)
        {
            int low = 0;
            int high = orderedIndices.Length;
            while (low < high)
            {
                int middleRank = low + (high - low) / 2;
                if (GetItemStart(orderedIndices[middleRank]) <= viewportEnd) low = middleRank + 1;
                else high = middleRank;
            }
            return low;
        }

        private void UpdateVisibleRange(int newStartRank, int newEndRankExclusive)
        {
            for (int rank = visibleStartRank; rank < visibleEndRankExclusive; rank++)
            {
                if (rank >= newStartRank && rank < newEndRankExclusive) continue;
                int index = orderedIndices[rank];
                T item = visibleItems[index];
                onHide(item);
                pool.Release(item);
                visibleItems.Remove(index);
            }

            for (int rank = newStartRank; rank < newEndRankExclusive; rank++)
            {
                int index = orderedIndices[rank];
                if (visibleItems.ContainsKey(index)) continue;
                T item = pool.Get();
                item.transform.SetParent(getParent(index), false);
                onShow(item, index);
                visibleItems.Add(index, item);
            }

            visibleStartRank = newStartRank;
            visibleEndRankExclusive = newEndRankExclusive;
        }
    }
}
