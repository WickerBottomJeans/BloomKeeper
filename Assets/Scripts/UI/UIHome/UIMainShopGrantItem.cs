using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIMainShopGrantItem : MonoBehaviour
    {
        [SerializeField] private Image grantIcon;
        [SerializeField] private TMP_Text grantQuantityText;

        public void DisplayShopGrant(ShopGrantViewData shopGrant, ShopSpriteCatalog shopSpriteCatalog)
        {
            grantIcon.sprite = shopSpriteCatalog.GetSprite(shopGrant.presentationKey);
            grantQuantityText.gameObject.SetActive(shopGrant.displayQuantity.HasValue);
            if (shopGrant.displayQuantity.HasValue) grantQuantityText.text = shopGrant.displayQuantity.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
