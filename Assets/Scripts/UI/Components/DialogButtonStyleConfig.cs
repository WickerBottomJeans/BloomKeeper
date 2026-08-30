using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    [CreateAssetMenu(fileName = "DialogButtonStyleConfig", menuName = "BloomKeeper/UI/Dialog Button Style Config")]
    public class DialogButtonStyleConfig : ScriptableObject
    {
        [SerializeField] private List<DialogButtonStyle> styles = new List<DialogButtonStyle>();

        public DialogButtonStyle GetStyle(DialogButtonColorVariant colorVariant)
        {
            foreach (DialogButtonStyle style in styles)
            {
                if (style.colorVariant == colorVariant)
                    return style;
            }

            throw new InvalidOperationException($"DialogButtonStyleConfig has no style for dialog button colorVariant: {colorVariant}.");
        }

        [Serializable]
        public class DialogButtonStyle
        {
            public DialogButtonColorVariant colorVariant;
            public Sprite sprite;
        }
    }
}
