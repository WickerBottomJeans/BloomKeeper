using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Displays one shop offer.
    /// </summary>
    public class UIMainShopCell : MonoBehaviour
    {
        [SerializeField] private TMP_Text offerNameText;
        [SerializeField] private Image costIcon;
        [SerializeField] private TMP_Text costQuantityText;
        [SerializeField] private Button buyButton;
        [SerializeField] private RectTransform grantItemRoot;
        [SerializeField] private UIMainShopGrantItem grantItemTemplate;

        private readonly List<UIMainShopGrantItem> activeGrantItems = new List<UIMainShopGrantItem>();
        private IObjectPool<UIMainShopGrantItem> grantItemPool;
        private string displayedOfferId;

        #region Unity Lifecycle

        private void Awake()
        {
            grantItemTemplate.gameObject.SetActive(false);
            grantItemPool = new ObjectPool<UIMainShopGrantItem>(CreateGrantItem, ShowGrantItem, HideGrantItem, DestroyGrantItem);
            buyButton.onClick.AddListener(HandleBuyRequested);
        }

        private void OnDisable()
        {
            ClearShopOffer();
        }

        private void OnDestroy()
        {
            buyButton.onClick.RemoveListener(HandleBuyRequested);
            grantItemPool.Clear();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Offer the player asked to buy.
        /// </summary>
        public event Action<string> BuyRequested;

        /// <summary>
        /// Displays an offer in this cell.
        /// </summary>
        public void DisplayShopOffer(ShopOfferDisplayData shopOfferDisplayData, ShopSpriteCatalog shopSpriteCatalog)
        {
            displayedOfferId = shopOfferDisplayData.OfferId;
            offerNameText.text = shopOfferDisplayData.DisplayName;
            costIcon.sprite = shopSpriteCatalog.GetSprite(shopOfferDisplayData.CostPresentationKey);
            costQuantityText.text = shopOfferDisplayData.CostText;

            // Clear grants from the previous offer.
            ClearGrantItems();
            foreach (ShopGrantDisplayData shopGrantDisplayData in shopOfferDisplayData.Grants)
            {
                UIMainShopGrantItem grantItem = grantItemPool.Get();
                grantItem.DisplayShopGrant(shopGrantDisplayData, shopSpriteCatalog);
                activeGrantItems.Add(grantItem);
            }
        }

        /// <summary>
        /// Clears the offer before this cell is reused.
        /// </summary>
        public void ClearShopOffer()
        {
            displayedOfferId = null;
            ClearGrantItems();
        }

        #endregion

        #region Private Methods

        private UIMainShopGrantItem CreateGrantItem()
        {
            UIMainShopGrantItem grantItem = Instantiate(grantItemTemplate, grantItemRoot);
            grantItem.gameObject.SetActive(false);
            return grantItem;
        }

        private void ShowGrantItem(UIMainShopGrantItem grantItem)
        {
            grantItem.gameObject.SetActive(true);
        }

        private void HideGrantItem(UIMainShopGrantItem grantItem)
        {
            grantItem.gameObject.SetActive(false);
        }

        private void DestroyGrantItem(UIMainShopGrantItem grantItem)
        {
            Destroy(grantItem.gameObject);
        }

        private void ClearGrantItems()
        {
            foreach (UIMainShopGrantItem grantItem in activeGrantItems)
                grantItemPool.Release(grantItem);

            activeGrantItems.Clear();
        }

        /// <summary>
        /// Sends the displayed offer ID when Buy is clicked.
        /// </summary>
        private void HandleBuyRequested()
        {
            BuyRequested?.Invoke(displayedOfferId);
        }

        #endregion
    }
}
