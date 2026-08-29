using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Displays the main shop offer list.
    /// </summary>
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
        private IReadOnlyList<ShopOfferDisplayData> displayedShopOfferDisplayDataList;
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

        /// <summary>
        /// Shop offer the player asked to buy.
        /// </summary>
        public event Action<string> BuyRequested;

        /// <summary>
        /// Displays prepared shop offers.
        /// </summary>
        public void DisplayMainShop(IReadOnlyList<ShopOfferDisplayData> shopOfferDisplayDataList)
        {
            if (shopOfferDisplayDataList == null) throw new ArgumentNullException(nameof(shopOfferDisplayDataList));

            // Clear cells from the previous shop display.
            DisposeShopCellPool();
            displayedShopOfferDisplayDataList = shopOfferDisplayDataList;
            shopCellHeight = ((RectTransform)shopCellTemplate.transform).rect.height;
            float shopContentHeight = topPadding + bottomPadding + displayedShopOfferDisplayDataList.Count * shopCellHeight + Mathf.Max(0, displayedShopOfferDisplayDataList.Count - 1) * cellSpacing;
            ShopCellRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, shopContentHeight);
            scrollRect.verticalNormalizedPosition = 1f;
            gameObject.SetActive(true);
            shopCellPool = new VerticalScrollPool<UIMainShopCell>(ShopCellRoot, viewport, scrollRect, shopCellTemplate, this, HandleShopCellCreated, DisplayShopCell, HideShopCell);
        }

        /// <summary>
        /// Hides the shop tab.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Listens for Buy requests from a new shop cell.
        /// </summary>
        private void HandleShopCellCreated(UIMainShopCell shopCell)
        {
            shopCell.BuyRequested += HandleBuyRequested;
        }

        /// <summary>
        /// Displays one offer in a visible shop cell.
        /// </summary>
        private void DisplayShopCell(UIMainShopCell shopCell, int offerIndex)
        {
            shopCell.DisplayShopOffer(displayedShopOfferDisplayDataList[offerIndex], shopSpriteCatalog);
        }

        /// <summary>
        /// Clears a shop cell that left the visible area.
        /// </summary>
        private void HideShopCell(UIMainShopCell shopCell)
        {
            shopCell.ClearShopOffer();
        }

        /// <summary>
        /// Raises BuyRequested with the selected offer ID.
        /// </summary>
        private void HandleBuyRequested(string offerId)
        {
            BuyRequested?.Invoke(offerId);
        }

        /// <summary>
        /// Clears the current shop cell pool.
        /// </summary>
        private void DisposeShopCellPool()
        {
            if (shopCellPool == null) return;

            shopCellPool.Dispose();
            shopCellPool = null;
            displayedShopOfferDisplayDataList = null;
            shopCellHeight = 0f;
        }

        int IScrollPoolGeometrySource.Count => displayedShopOfferDisplayDataList.Count;

        ScrollPoolItemGeometry IScrollPoolGeometrySource.GetGeometry(int index)
        {
            float shopCellCenterY = -topPadding - shopCellHeight / 2f - index * (shopCellHeight + cellSpacing);
            return new ScrollPoolItemGeometry(new Vector2(0f, shopCellCenterY), shopCellHeight / 2f);
        }

        #endregion
    }
}
