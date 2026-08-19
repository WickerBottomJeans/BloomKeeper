using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIMainShopTab : MonoBehaviour, IScrollPoolGeometrySource
    {
        [SerializeField] private RectTransform ShopCellRoot;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private UIMainShopCell shopCellTemplate;
        [SerializeField] private ShopSpriteCatalog shopSpriteCatalog;
        [SerializeField] private float topPadding;
        [SerializeField] private float bottomPadding;
        [SerializeField] private float cellSpacing;

        private VerticalScrollPool<UIMainShopCell> shopCellPool;
        private IReadOnlyList<ShopOfferViewData> displayedShopOffers;
        private float shopCellHeight;

        #region Unity Lifecycle

        private void Awake()
        {
            shopCellTemplate.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DisposeShopCellPool();
        }

        #endregion

        #region Public API

        public void DisplayMainShop(LoadShopResponse mainShopResponse)
        {
            if (mainShopResponse == null) throw new ArgumentNullException(nameof(mainShopResponse));
            if (mainShopResponse.offers == null) throw new ArgumentException("Main shop response has no offers.", nameof(mainShopResponse));

            DisposeShopCellPool();
            displayedShopOffers = mainShopResponse.offers;
            shopCellHeight = ((RectTransform)shopCellTemplate.transform).rect.height;
            float shopContentHeight = topPadding + bottomPadding + displayedShopOffers.Count * shopCellHeight + Mathf.Max(0, displayedShopOffers.Count - 1) * cellSpacing;
            ShopCellRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, shopContentHeight);
            scrollRect.verticalNormalizedPosition = 1f;
            gameObject.SetActive(true);
            shopCellPool = new VerticalScrollPool<UIMainShopCell>(ShopCellRoot, viewport, scrollRect, shopCellTemplate, this, null, DisplayShopCell, HideShopCell);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void DisplayShopCell(UIMainShopCell shopCell, int offerIndex)
        {
            shopCell.DisplayShopOffer(displayedShopOffers[offerIndex], shopSpriteCatalog);
        }

        private void HideShopCell(UIMainShopCell shopCell)
        {
            shopCell.ClearShopOffer();
        }

        private void DisposeShopCellPool()
        {
            if (shopCellPool == null) return;

            shopCellPool.Dispose();
            shopCellPool = null;
            displayedShopOffers = null;
            shopCellHeight = 0f;
        }

        int IScrollPoolGeometrySource.Count => displayedShopOffers.Count;

        ScrollPoolItemGeometry IScrollPoolGeometrySource.GetGeometry(int index)
        {
            float shopCellCenterY = -topPadding - shopCellHeight / 2f - index * (shopCellHeight + cellSpacing);
            return new ScrollPoolItemGeometry(new Vector2(0f, shopCellCenterY), shopCellHeight / 2f);
        }

        #endregion
    }
}
