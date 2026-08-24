using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Displays one item granted by a shop offer.
    /// </summary>
    public class UIMainShopGrantItem : MonoBehaviour
    {
        [SerializeField] private Image grantIcon;
        [SerializeField] private TMP_Text grantQuantityText;

        /// <summary>
        /// Displays a shop grant's icon and quantity.
        /// </summary>
        public void DisplayShopGrant(ShopGrantViewData shopGrant, ShopSpriteCatalog shopSpriteCatalog)
        {
            grantIcon.sprite = shopSpriteCatalog.GetSprite(shopGrant.presentationKey);
            grantQuantityText.gameObject.SetActive(shopGrant.displayQuantity.HasValue);
            if (shopGrant.displayQuantity.HasValue) grantQuantityText.text = shopGrant.displayQuantity.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
