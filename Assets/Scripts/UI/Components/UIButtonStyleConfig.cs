using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    [CreateAssetMenu(fileName = "UIButtonStyleConfig", menuName = "BloomKeeper/UI/Button Style Config")]
    public class UIButtonStyleConfig : ScriptableObject
    {
        [SerializeField] private List<ButtonStyle> styles = new List<ButtonStyle>();

        public ButtonStyle GetStyle(UIButtonVariant variant)
        {
            foreach (ButtonStyle style in styles)
            {
                if (style.variant == variant)
                    return style;
            }

            throw new InvalidOperationException($"UIButtonStyleConfig has no style for button variant: {variant}.");
        }

        [Serializable]
        public class ButtonStyle
        {
            public UIButtonVariant variant;
            public Sprite sprite;
        }
    }
}
