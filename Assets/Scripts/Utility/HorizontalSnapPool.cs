using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class HorizontalSnapPool : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField, Min(0f)] private float snapDuration = 0.2f;
        [SerializeField] private Ease snapEase = Ease.OutCubic;
        [SerializeField] private float minimumSwipeSpeedInItemsPerSecond = 0.5f;

        private ScrollRect scrollRect;
        private RectTransform viewport;
        private RectTransform content;
        private IDisposable viewPool;
        private Action<int, int> updateVisibleViews;
        private Action repositionVisibleViews;
        private Tween snapTween;
        private int itemCount;
        private int preloadItemCount;
        private int dragStartIndex;
        
        /// <summary>
        /// The horizontal distance between neighboring item centers.
        /// </summary>
        private float itemStep;
        private bool isInitialized;
        private bool isRefreshingViewport;

        public int CurrentIndex { get; private set; } = -1;
        public event Action<int> CurrentIndexChanged;

        private void Awake()
        {
            snapDuration = Mathf.Max(0f, snapDuration);
            if (minimumSwipeSpeedInItemsPerSecond < 0f)
                throw new InvalidOperationException("Horizontal snap pool minimum swipe speed cannot be negative.");

            scrollRect = GetComponent<ScrollRect>();
            viewport = scrollRect.viewport;
            content = scrollRect.content;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            content.anchorMin = new Vector2(0f, content.anchorMin.y);
            content.anchorMax = new Vector2(0f, content.anchorMax.y);
            content.pivot = new Vector2(0f, content.pivot.y);
            isInitialized = true;
        }

        private void OnEnable()
        {
            scrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
        }

        private void OnDisable()
        {
            scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
            StopMotion();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isInitialized || itemCount == 0 || isRefreshingViewport) return;
            RefreshViewport();
        }

        private void OnDestroy()
        {
            ClearPoolAndConfig();
        }

        public void Configure<T>(T prefab, int itemCount, float itemStep, Action<T> onCreate, Action<T, int> onShow,
            Action<T> onHide, int initialIndex = 0, int defaultCapacity = 3, int maxSize = 5, int preloadItemCount = 1)
            where T : Component
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (itemCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemCount), itemCount,
                    "Horizontal snap pool requires at least one item. Use ClearPoolAndConfig() for the empty state.");
            if (itemStep <= 0f)
                throw new ArgumentOutOfRangeException(nameof(itemStep), itemStep,
                    "Horizontal snap pool item step must be positive.");
            if (initialIndex < 0 || initialIndex >= itemCount)
                throw new ArgumentOutOfRangeException(nameof(initialIndex), initialIndex,
                    $"Initial index must be between 0 and {itemCount - 1}.");
            if (preloadItemCount < 0)
                throw new ArgumentOutOfRangeException(nameof(preloadItemCount), preloadItemCount,
                    "Horizontal snap pool preload count cannot be negative.");

            
            ClearPoolAndConfig();
            this.itemCount = itemCount;
            this.itemStep = itemStep;
            this.preloadItemCount = preloadItemCount;

            try
            {
                ViewPool<T> typedViewPool = new ViewPool<T>(content, viewport, prefab, itemStep, onCreate, onShow,
                    onHide, defaultCapacity, maxSize);
                viewPool = typedViewPool;
                updateVisibleViews = typedViewPool.UpdateVisibleViews;
                repositionVisibleViews = typedViewPool.RepositionVisibleViews;

                UpdateContentWidth();
                SetContentPosition(initialIndex);
                SetCurrentIndex(initialIndex);
                RefreshVisibleViews();
            }
            catch
            {
                ClearPoolAndConfig();
                throw;
            }
        }

        public void OnBeginDrag(PointerEventData _)
        {
            if (itemCount == 0) return;
            StopMotion();
            dragStartIndex = CalculateNearestIndex();
            SetCurrentIndex(dragStartIndex);
        }

        public void OnEndDrag(PointerEventData _)
        {
            if (itemCount == 0) return;

            float swipeSpeedInItemsPerSecond = scrollRect.velocity.x / itemStep;
            if (Mathf.Abs(swipeSpeedInItemsPerSecond) < minimumSwipeSpeedInItemsPerSecond)
            {
                SnapToIndex(CalculateNearestIndex());
                return;
            }

            int direction = swipeSpeedInItemsPerSecond < 0f ? 1 : -1;
            SnapToIndex(Mathf.Clamp(dragStartIndex + direction, 0, itemCount - 1));
        }

        public void JumpToIndex(int index)
        {
            ValidateIndex(index);
            StopMotion();
            SetContentPosition(index);
            SetCurrentIndex(index);
            RefreshVisibleViews();
        }

        public void SnapToIndex(int index)
        {
            ValidateIndex(index);
            StopMotion();

            float targetX = -index * itemStep;
            if (snapDuration == 0f)
            {
                content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);
                SetCurrentIndex(index);
                RefreshVisibleViews();
                return;
            }

            snapTween = content.DOAnchorPosX(targetX, snapDuration).SetEase(snapEase).SetUpdate(true)
                .SetLink(gameObject).OnComplete(() =>
                {
                    snapTween = null;
                    SetCurrentIndex(index);
                    RefreshVisibleViews();
                });
        }

        public void RefreshViewport()
        {
            if (itemCount == 0) return;

            isRefreshingViewport = true;
            try
            {
                StopMotion();
                UpdateContentWidth();
                repositionVisibleViews();
                SetContentPosition(CurrentIndex);
                RefreshVisibleViews();
            }
            finally
            {
                isRefreshingViewport = false;
            }
        }

        /// <summary>
        /// Destroys all pooled views and resets the pool config
        /// </summary>
        public void ClearPoolAndConfig()
        {
            StopMotion();
            viewPool?.Dispose();
            viewPool = null;
            updateVisibleViews = null;
            repositionVisibleViews = null;
            itemCount = 0;
            itemStep = 0f;
            preloadItemCount = 0;
            dragStartIndex = 0;
            CurrentIndex = -1;

            if (!isInitialized) return;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
            content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);
        }

        private void HandleScrollValueChanged(Vector2 _)
        {
            if (itemCount == 0) return;
            SetCurrentIndex(CalculateNearestIndex());
            RefreshVisibleViews();
        }

        private int CalculateNearestIndex()
        {
            return Mathf.Clamp(Mathf.RoundToInt(-content.anchoredPosition.x / itemStep), 0, itemCount - 1);
        }
        
        /// <summary>
        /// Calculates the visible range and rents or releases views to match it.
        /// </summary>
        private void RefreshVisibleViews()
        {
            float viewportWidth = GetViewportWidth();
            float viewportLeft = -content.anchoredPosition.x;
            float firstItemCenter = viewportWidth / 2f;
            int firstVisibleIndex =
                Mathf.Clamp(Mathf.CeilToInt((viewportLeft - firstItemCenter) / itemStep) - preloadItemCount, 0,
                    itemCount - 1);
            int lastVisibleIndex =
                Mathf.Clamp(
                    Mathf.FloorToInt((viewportLeft + viewportWidth - firstItemCenter) / itemStep) + preloadItemCount, 0,
                    itemCount - 1);
            updateVisibleViews(firstVisibleIndex, lastVisibleIndex);
        }

        private void UpdateContentWidth()
        {
            float contentWidth = GetViewportWidth() + (itemCount - 1) * itemStep;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        }

        /// <summary>
        /// Moves the content to center the item at this index.
        /// </summary>
        private void SetContentPosition(int index)
        {
            content.anchoredPosition = new Vector2(-index * itemStep, content.anchoredPosition.y);
        }

        private void SetCurrentIndex(int index)
        {
            if (CurrentIndex == index) return;
            CurrentIndex = index;
            CurrentIndexChanged?.Invoke(index);
        }

        private float GetViewportWidth()
        {
            float viewportWidth = viewport.rect.width;
            if (viewportWidth <= 0f)
                throw new InvalidOperationException("Horizontal snap pool requires a positive viewport width.");
            return viewportWidth;
        }

        private void StopMotion()
        {
            snapTween?.Kill();
            snapTween = null;
            if (isInitialized) scrollRect.StopMovement();
        }

        private void ValidateIndex(int index)
        {
            if (itemCount == 0)
                throw new InvalidOperationException(
                    "Horizontal snap pool must be configured before selecting an item.");
            if (index < 0 || index >= itemCount)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Index must be between 0 and {itemCount - 1}.");
        }

        private class ViewPool<T> : IDisposable where T : Component
        {
            private readonly RectTransform content;
            private readonly RectTransform viewport;
            private readonly float itemStep;
            private readonly Action<T, int> onShow;
            private readonly Action<T> onHide;
            private readonly ObjectPool<T> pool;
            private readonly Dictionary<int, T> visibleViews = new Dictionary<int, T>();
            private int firstVisibleIndex;
            private int lastVisibleIndex = -1;

            public ViewPool(RectTransform content, RectTransform viewport, T prefab, float itemStep, Action<T> onCreate,
                Action<T, int> onShow, Action<T> onHide, int defaultCapacity, int maxSize)
            {
                this.content = content;
                this.viewport = viewport;
                this.itemStep = itemStep;
                this.onShow = onShow;
                this.onHide = onHide;
                pool = new ObjectPool<T>(() =>
                    {
                        T view = UnityEngine.Object.Instantiate(prefab, content);
                        try
                        {
                            RectTransform viewTransform = view.transform as RectTransform;
                            if (viewTransform == null)
                                throw new InvalidOperationException(
                                    $"Horizontal snap pool view '{typeof(T).Name}' requires a RectTransform on its root GameObject.");
                            viewTransform.anchorMin = new Vector2(0f, 0.5f);
                            viewTransform.anchorMax = new Vector2(0f, 0.5f);
                            view.gameObject.SetActive(false);
                            onCreate?.Invoke(view);
                            return view;
                        }
                        catch
                        {
                            UnityEngine.Object.Destroy(view.gameObject);
                            throw;
                        }
                    }, view => view.gameObject.SetActive(true), view => view.gameObject.SetActive(false),
                    view => UnityEngine.Object.Destroy(view.gameObject), true, defaultCapacity, maxSize);
            }

            /// <summary>
            /// Rents and releases views for the visible range.
            /// </summary>
            public void UpdateVisibleViews(int newFirstVisibleIndex, int newLastVisibleIndex)
            {
                for (int index = firstVisibleIndex; index <= lastVisibleIndex; index++)
                {
                    if (index >= newFirstVisibleIndex && index <= newLastVisibleIndex) continue;
                    T view = visibleViews[index];
                    onHide(view);
                    pool.Release(view);
                    visibleViews.Remove(index);
                }

                for (int index = newFirstVisibleIndex; index <= newLastVisibleIndex; index++)
                {
                    if (visibleViews.ContainsKey(index)) continue;
                    T view = pool.Get();
                    try
                    {
                        PositionView(view, index);
                        onShow(view, index);
                        visibleViews.Add(index, view);
                    }
                    catch
                    {
                        pool.Release(view);
                        throw;
                    }
                }

                firstVisibleIndex = newFirstVisibleIndex;
                lastVisibleIndex = newLastVisibleIndex;
            }

            /// <summary>
            /// Moves every visible view to its correct position.
            /// </summary>
            public void RepositionVisibleViews()
            {
                foreach (KeyValuePair<int, T> visibleView in visibleViews)
                    PositionView(visibleView.Value, visibleView.Key);
            }
            
            /// <summary>
            /// Positions a view in the content slot for its item index.
            /// </summary>
            private void PositionView(T view, int index)
            {
                RectTransform viewTransform = (RectTransform)view.transform;
                viewTransform.anchoredPosition = new Vector2(viewport.rect.width / 2f + index * itemStep, 0f);
            }

            public void Dispose()
            {
                foreach (T view in visibleViews.Values)
                {
                    onHide(view);
                    pool.Release(view);
                }

                visibleViews.Clear();
                pool.Clear();
            }
        }
    }
}
