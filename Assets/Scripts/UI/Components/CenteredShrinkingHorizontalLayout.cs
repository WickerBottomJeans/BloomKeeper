using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// [Duong] Keeps direct children centered at their maximum size, then shrinks them evenly to fit.
    /// </summary>
    public class CenteredShrinkingHorizontalLayout : LayoutGroup
    {
        [SerializeField] private float maximumChildSize;
        [SerializeField] private float childSpacing;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(0f, 0f, 0f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            SetLayoutInputForAxis(0f, 0f, 0f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            LayoutChildren();
        }

        public override void SetLayoutVertical()
        {
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            // [Duong] Skip empty layout content.
            if (rectChildren.Count == 0) return;

            // [Duong] Validate layout tuning.
            if (maximumChildSize <= 0f) throw new InvalidOperationException("Centered shrinking horizontal layout maximum child size must be greater than zero.");
            if (childSpacing < 0f) throw new InvalidOperationException("Centered shrinking horizontal layout child spacing cannot be negative.");
            float innerWidth = rectTransform.rect.width - padding.horizontal;
            float innerHeight = rectTransform.rect.height - padding.vertical;
            if (innerWidth <= 0f || innerHeight <= 0f) return;

            // [Duong] Fit and center the child cluster.
            int childCount = rectChildren.Count;
            float totalSpacing = childSpacing * (childCount - 1);
            float maximumWidthPerChild = (innerWidth - totalSpacing) / childCount;
            float childSize = Mathf.Min(maximumChildSize, innerHeight, maximumWidthPerChild);
            if (childSize <= 0f) throw new InvalidOperationException("Centered shrinking horizontal layout has no room for its child spacing and children.");

            float clusterWidth = childSize * childCount + totalSpacing;
            float firstItemX = padding.left + (innerWidth - clusterWidth) * 0.5f;
            float childY = padding.top + (innerHeight - childSize) * 0.5f;

            // [Duong] Position every child.
            for (int i = 0; i < childCount; i++)
            {
                RectTransform child = rectChildren[i];
                SetChildAlongAxis(child, 0, firstItemX + i * (childSize + childSpacing), childSize);
                SetChildAlongAxis(child, 1, childY, childSize);
            }
        }
    }
}
