using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class UISliderFill : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private RectMask2D fillMask;

        private void OnEnable()
        {
            slider.onValueChanged.AddListener(DisplayFill);
            DisplayFill(slider.normalizedValue);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(DisplayFill);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled) return;

            DisplayFill(slider.normalizedValue);
        }

        private void DisplayFill(float normalizedValue)
        {
            RectTransform maskRect = (RectTransform)fillMask.transform;
            float hiddenWidth = maskRect.rect.width * (1f - normalizedValue);
            fillMask.padding = new Vector4(0f, 0f, hiddenWidth, 0f);
        }
    }
}
