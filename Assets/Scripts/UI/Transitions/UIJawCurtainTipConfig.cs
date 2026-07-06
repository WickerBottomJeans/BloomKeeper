using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    [CreateAssetMenu(fileName = "UIJawCurtainTipConfig", menuName = "BloomKeeper/UI/Jaw Curtain Tip Config")]
    public class UIJawCurtainTipConfig : ScriptableObject
    {
        [SerializeField] private List<UIJawCurtainTip> tips = new();

        public int TipCount => tips.Count;

        public UIJawCurtainTip GetTip(int index)
        {
            return tips[index];
        }

        public bool IsTipInCategory(int index, UIJawCurtainTipCategory category)
        {
            return tips[index].Category == category;
        }
    }

    [Serializable]
    public struct UIJawCurtainTip
    {
        [SerializeField] private UIJawCurtainTipCategory category;
        [SerializeField, TextArea] private string text;
        [SerializeField] private Sprite sprite;

        public UIJawCurtainTipCategory Category => category;
        public string Text => text;
        public Sprite Sprite => sprite;

        public UIJawCurtainTip(UIJawCurtainTipCategory category, string text, Sprite sprite)
        {
            this.category = category;
            this.text = text;
            this.sprite = sprite;
        }
    }
}
