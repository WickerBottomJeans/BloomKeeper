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
        [SerializeField] private TMP_Text grantDisplayText;

        /// <summary>
        /// Displays a shop grant's icon and prepared text.
        /// </summary>
        public void DisplayShopGrant(ShopGrantDisplayData shopGrantDisplayData, ShopSpriteCatalog shopSpriteCatalog)
        {
            grantIcon.sprite = shopSpriteCatalog.GetSprite(shopGrantDisplayData.PresentationKey);
            grantDisplayText.gameObject.SetActive(true);
            grantDisplayText.text = shopGrantDisplayData.DisplayText;
        }
    }
}
