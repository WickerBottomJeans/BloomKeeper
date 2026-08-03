using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    [CreateAssetMenu(fileName = "DialogButtonStyleConfig", menuName = "BloomKeeper/UI/Dialog Button Style Config")]
    public class DialogButtonStyleConfig : ScriptableObject
    {
        [SerializeField] private List<DialogButtonStyle> styles = new List<DialogButtonStyle>();

        public DialogButtonStyle GetStyle(DialogButtonVariant variant)
        {
            foreach (DialogButtonStyle style in styles)
            {
                if (style.variant == variant)
                    return style;
            }

            throw new InvalidOperationException($"DialogButtonStyleConfig has no style for dialog button variant: {variant}.");
        }

        [Serializable]
        public class DialogButtonStyle
        {
            public DialogButtonVariant variant;
            public Sprite sprite;
        }
    }
}
