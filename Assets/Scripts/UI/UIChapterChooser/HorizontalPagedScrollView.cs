using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public sealed class HorizontalPagedScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform pages;
        [SerializeField] private GridLayoutGroup pageGrid;
        [SerializeField] private float snapDuration = 0.2f;
        [SerializeField] private Ease snapEase = Ease.OutCubic;
        [SerializeField] private float minimumSwipeSpeedInPagesPerSecond = 0.5f;

        private ScrollRect scrollRect;
        private Tween snapTween;
        private int pageCount;
        private int dragStartPage;
        private bool initialized;
        private readonly List<RectTransform> pageSlots = new();

        public int CurrentPage { get; private set; }
        public RectTransform Pages => pages;
        public RectTransform Viewport => viewport;
        public ScrollRect ScrollRect => scrollRect;
        public float PageWidth => viewport.rect.width;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            initialized = true;
            ApplyPageSize();
        }

        private void OnEnable()
        {
            scrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
        }

        private void OnDisable()
        {
            scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
            snapTween?.Kill();
            snapTween = null;
        }

        public void RefreshPages(int pageCount)
        {
            ClearPageSlots();
            this.pageCount = pageCount;
            CurrentPage = pageCount == 0 ? 0 : Mathf.Clamp(CurrentPage, 0, pageCount - 1);
            for (int index = 0; index < pageCount; index++)
            {
                var slotObject = new GameObject($"Page {index}", typeof(RectTransform));
                RectTransform slot = slotObject.GetComponent<RectTransform>();
                slot.SetParent(pages, false);
                pageSlots.Add(slot);
            }
            ApplyPageSize();
            LayoutRebuilder.ForceRebuildLayoutImmediate(pages);
            SetPagePosition(CurrentPage);
        }

        public RectTransform GetPageSlot(int pageIndex)
        {
            return pageSlots[pageIndex];
        }

        public Vector2 GetPagePosition(int pageIndex)
        {
            return new Vector2((pageIndex + 0.5f) * viewport.rect.width, 0f);
        }

        public void SetPage(int pageIndex, bool animated)
        {
            if (pageCount == 0)
            {
                SetPagePosition(0);
                return;
            }

            int clampedPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            SetCurrentPage(clampedPageIndex);
            snapTween?.Kill();
            snapTween = null;

            float targetX = -CurrentPage * viewport.rect.width;
            if (!animated)
            {
                pages.anchoredPosition = new Vector2(targetX, pages.anchoredPosition.y);
                return;
            }

            scrollRect.StopMovement();
            snapTween = pages.DOAnchorPosX(targetX, snapDuration).SetEase(snapEase).SetUpdate(true).SetLink(gameObject).OnComplete(() => snapTween = null);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            snapTween?.Kill();
            snapTween = null;
            dragStartPage = CurrentPage;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float swipeSpeedInPagesPerSecond = scrollRect.velocity.x / viewport.rect.width;
            scrollRect.StopMovement();
            if (Mathf.Abs(swipeSpeedInPagesPerSecond) >= minimumSwipeSpeedInPagesPerSecond)
            {
                int pageDirection = swipeSpeedInPagesPerSecond < 0f ? 1 : -1;
                SetPage(dragStartPage + pageDirection, true);
                return;
            }

            SetPage(CalculateNearestPage(), true);
        }

        private void HandleScrollValueChanged(Vector2 normalizedPosition)
        {
            if (pageCount == 0 || viewport.rect.width <= 0f) return;
            SetCurrentPage(CalculateNearestPage());
        }

        private int CalculateNearestPage()
        {
            return Mathf.Clamp(Mathf.RoundToInt(-pages.anchoredPosition.x / viewport.rect.width), 0, pageCount - 1);
        }

        private void SetCurrentPage(int pageIndex)
        {
            if (CurrentPage == pageIndex) return;
            CurrentPage = pageIndex;
        }

        private void ApplyPageSize()
        {
            pageGrid.cellSize = viewport.rect.size;
        }

        private void SetPagePosition(int pageIndex)
        {
            snapTween?.Kill();
            snapTween = null;
            scrollRect.StopMovement();
            pages.anchoredPosition = new Vector2(-pageIndex * viewport.rect.width, pages.anchoredPosition.y);
        }

        private void ClearPageSlots()
        {
            foreach (RectTransform slot in pageSlots)
            {
                if (slot == null) continue;
                slot.gameObject.SetActive(false);
                Destroy(slot.gameObject);
            }
            pageSlots.Clear();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!initialized) return;
            ApplyPageSize();
            LayoutRebuilder.ForceRebuildLayoutImmediate(pages);
            SetPagePosition(CurrentPage);
        }
    }
}
