using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIBackground : MonoBehaviour
    {
        [SerializeField] private RawImage backgroundImage;

        public void Show(Texture backgroundTexture)
        {
            backgroundImage.texture = backgroundTexture;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
