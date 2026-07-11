using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class TipBoard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image image;

        public void SetTip(string text, Sprite sprite = null)
        {
            bool hasText = !string.IsNullOrWhiteSpace(text);
            bool hasSprite = sprite != null;

            gameObject.SetActive(hasText || hasSprite);
            label.text = text;
            label.gameObject.SetActive(hasText);
            image.sprite = sprite;
            image.gameObject.SetActive(hasSprite);
        }
    }
}
