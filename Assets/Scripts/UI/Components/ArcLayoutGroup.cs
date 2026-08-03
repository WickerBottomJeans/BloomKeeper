using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    [AddComponentMenu("Layout/Arc Layout Group")]
    public class ArcLayoutGroup : LayoutGroup
    {
        [SerializeField, Tooltip("The RectTransform whose center and dimensions define the elliptical arc.")]
        private RectTransform arcBounds;
        
        [SerializeField] private Vector2 preferredCellSize = new Vector2(32f, 32f);
        
        [SerializeField, Tooltip("Angle in degrees at the center of the allowed arc. Zero points right and 90 points up.")]
        private float arcCenterAngle = 160f;
        
        [SerializeField, Tooltip("Preferred angle in degrees between active children. Positive values move counterclockwise and negative values move clockwise.")]
        private float preferredAngleSpacing = -35f;
        
        [SerializeField, Range(0f, 360f), Tooltip("The total angle in degrees available around the arc center angle.")]
        private float arcSpan = 140f;
        
        [SerializeField] private ArcLayoutAlignment alignment = ArcLayoutAlignment.Start;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float requiredWidth = arcBounds.rect.width + preferredCellSize.x;
            SetLayoutInputForAxis(requiredWidth, requiredWidth, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float requiredHeight = arcBounds.rect.height + preferredCellSize.y;
            SetLayoutInputForAxis(requiredHeight, requiredHeight, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            int layoutIndex = 0;
            int childCount = GetLayoutChildCount();
            float effectiveAngleSpacing = GetEffectiveAngleSpacing(childCount);
            float effectiveStartAngle = GetEffectiveStartAngle(childCount, effectiveAngleSpacing);
            Vector2 effectiveCellSize = GetEffectiveCellSize(childCount, effectiveStartAngle, effectiveAngleSpacing);
            Rect parentRect = rectTransform.rect;
            foreach (RectTransform child in rectChildren)
            {
                if (child == arcBounds) continue;
                Vector2 targetPosition = GetChildPosition(layoutIndex, effectiveStartAngle, effectiveAngleSpacing);
                float positionFromLeft = targetPosition.x - parentRect.xMin - effectiveCellSize.x * 0.5f;
                SetChildAlongAxis(child, 0, positionFromLeft, effectiveCellSize.x);
                layoutIndex++;
            }
        }

        public override void SetLayoutVertical()
        {
            int layoutIndex = 0;
            int childCount = GetLayoutChildCount();
            float effectiveAngleSpacing = GetEffectiveAngleSpacing(childCount);
            float effectiveStartAngle = GetEffectiveStartAngle(childCount, effectiveAngleSpacing);
            Vector2 effectiveCellSize = GetEffectiveCellSize(childCount, effectiveStartAngle, effectiveAngleSpacing);
            Rect parentRect = rectTransform.rect;
            foreach (RectTransform child in rectChildren)
            {
                if (child == arcBounds) continue;
                Vector2 targetPosition = GetChildPosition(layoutIndex, effectiveStartAngle, effectiveAngleSpacing);
                float positionFromTop = parentRect.yMax - targetPosition.y - effectiveCellSize.y * 0.5f;
                SetChildAlongAxis(child, 1, positionFromTop, effectiveCellSize.y);
                layoutIndex++;
            }
        }

        private int GetLayoutChildCount()
        {
            int childCount = 0;
            foreach (RectTransform child in rectChildren)
                if (child != arcBounds) childCount++;
            return childCount;
        }

        private float GetEffectiveAngleSpacing(int childCount)
        {
            if (childCount <= 1) return preferredAngleSpacing;
            float spacingMagnitude = Mathf.Min(Mathf.Abs(preferredAngleSpacing), arcSpan / (childCount - 1));
            return Mathf.Sign(preferredAngleSpacing) * spacingMagnitude;
        }

        private float GetEffectiveStartAngle(int childCount, float effectiveAngleSpacing)
        {
            float direction = Mathf.Sign(effectiveAngleSpacing);
            float usedArcSpan = Mathf.Abs(effectiveAngleSpacing) * Mathf.Max(0, childCount - 1);
            float unusedArcSpan = arcSpan - usedArcSpan;
            float alignmentOffset = alignment switch
            {
                ArcLayoutAlignment.Start => 0f,
                ArcLayoutAlignment.Center => unusedArcSpan * 0.5f,
                ArcLayoutAlignment.End => unusedArcSpan,
                _ => throw new System.ArgumentOutOfRangeException(nameof(alignment), alignment, "Unsupported arc layout alignment.")
            };
            float arcStartAngle = arcCenterAngle - direction * arcSpan * 0.5f;
            return arcStartAngle + direction * alignmentOffset;
        }

        private Vector2 GetEffectiveCellSize(int childCount, float effectiveStartAngle, float effectiveAngleSpacing)
        {
            float scale = 1f;
            for (int firstIndex = 0; firstIndex < childCount; firstIndex++)
            {
                Vector2 firstPosition = GetChildPosition(firstIndex, effectiveStartAngle, effectiveAngleSpacing);
                for (int secondIndex = firstIndex + 1; secondIndex < childCount; secondIndex++)
                {
                    Vector2 secondPosition = GetChildPosition(secondIndex, effectiveStartAngle, effectiveAngleSpacing);
                    Vector2 separation = secondPosition - firstPosition;
                    float nonOverlappingScale = Mathf.Max(Mathf.Abs(separation.x) / preferredCellSize.x, Mathf.Abs(separation.y) / preferredCellSize.y);
                    scale = Mathf.Min(scale, nonOverlappingScale);
                }
            }
            return preferredCellSize * scale;
        }

        private Vector2 GetChildPosition(int childIndex, float effectiveStartAngle, float effectiveAngleSpacing)
        {
            float angleInRadians = (effectiveStartAngle + childIndex * effectiveAngleSpacing) * Mathf.Deg2Rad;
            Rect bounds = arcBounds.rect;
            Vector2 positionInBounds = bounds.center + new Vector2(Mathf.Cos(angleInRadians) * bounds.width * 0.5f, Mathf.Sin(angleInRadians) * bounds.height * 0.5f);
            return rectTransform.InverseTransformPoint(arcBounds.TransformPoint(positionInBounds));
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
