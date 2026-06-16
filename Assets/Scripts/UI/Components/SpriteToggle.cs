using UnityEngine;
using UnityEngine.UI;

namespace UI.Components
{
    public class SpriteToggle : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;

        private void Awake()
        {
            if (toggle == null) return;
            toggle.onValueChanged.AddListener(OnValueChanged);
            OnValueChanged(toggle.isOn);
        }

        private void OnValueChanged(bool isOn)
        {
            if (targetImage == null) return;
            targetImage.sprite = isOn ? onSprite : offSprite;
        }

        private void OnDestroy()
        {
            if (toggle == null) return;
            toggle.onValueChanged.RemoveListener(OnValueChanged);
        }
    }
}