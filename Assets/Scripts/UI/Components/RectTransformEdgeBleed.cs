using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformEdgeBleed : MonoBehaviour
    {
        [SerializeField] private bool bleedLeft;
        [SerializeField] private bool bleedRight;
        [SerializeField] private bool bleedTop;
        [SerializeField] private bool bleedBottom;

        private readonly Vector3[] drivenWorldCorners = new Vector3[4];
        private readonly Vector3[] targetWorldCorners = new Vector3[4];

        /// <summary>
        /// The rect being stretched
        /// </summary>
        private RectTransform drivenRect;

        /// <summary>
        /// The rect to stretch toward
        /// </summary>
        private RectTransform targetRect;
        private bool hasTarget;
        private bool subscribed;

        private void Awake()
        {
            drivenRect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (!hasTarget) return;
            Subscribe();
            ApplyBleed();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SetTarget(RectTransform target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            drivenRect ??= GetComponent<RectTransform>();
            RectTransform parentRect = GetParentRect();
            ValidateConfiguration(parentRect, target);
            targetRect = target;
            hasTarget = true;
            if (!isActiveAndEnabled) return;

            Subscribe();
            ApplyBleed();
        }

        private void Subscribe()
        {
            if (subscribed) return;
            Canvas.willRenderCanvases += ApplyBleed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            Canvas.willRenderCanvases -= ApplyBleed;
            subscribed = false;
        }

        private void ApplyBleed()
        {
            RectTransform parentRect = GetParentRect();
            ValidateConfiguration(parentRect, targetRect);

            Vector2 drivenMin = parentRect.InverseTransformPoint(drivenWorldCorners[0]);
            Vector2 drivenMax = parentRect.InverseTransformPoint(drivenWorldCorners[2]);
            Vector2 targetMin = parentRect.InverseTransformPoint(targetWorldCorners[0]);
            Vector2 targetMax = parentRect.InverseTransformPoint(targetWorldCorners[2]);
            Vector2 offsetMin = drivenRect.offsetMin;
            Vector2 offsetMax = drivenRect.offsetMax;

            if (bleedLeft) offsetMin.x += targetMin.x - drivenMin.x;
            if (bleedRight) offsetMax.x += targetMax.x - drivenMax.x;
            if (bleedBottom) offsetMin.y += targetMin.y - drivenMin.y;
            if (bleedTop) offsetMax.y += targetMax.y - drivenMax.y;

            bool offsetMinChanged = offsetMin != drivenRect.offsetMin;
            bool offsetMaxChanged = offsetMax != drivenRect.offsetMax;
            if (offsetMinChanged) drivenRect.offsetMin = offsetMin;
            if (offsetMaxChanged) drivenRect.offsetMax = offsetMax;
        }

        private RectTransform GetParentRect()
        {
            if (drivenRect.parent is not RectTransform parentRect) throw new InvalidOperationException($"{nameof(RectTransformEdgeBleed)} on '{name}' requires a RectTransform parent.");
            return parentRect;
        }

        private void ValidateConfiguration(RectTransform parentRect, RectTransform target)
        {
            if (!bleedLeft && !bleedRight && !bleedTop && !bleedBottom) throw new InvalidOperationException($"{nameof(RectTransformEdgeBleed)} on '{name}' requires at least one enabled bleed edge.");
            if (target == drivenRect) throw new InvalidOperationException($"{nameof(RectTransformEdgeBleed)} on '{name}' cannot use its driven RectTransform as its target.");
            Vector3 drivenScale = drivenRect.localScale;
            if (!Mathf.Approximately(drivenScale.x, 1f) || !Mathf.Approximately(drivenScale.y, 1f)) throw new InvalidOperationException($"{nameof(RectTransformEdgeBleed)} on '{name}' requires its driven RectTransform to have a local X/Y scale of one.");

            drivenRect.GetWorldCorners(drivenWorldCorners);
            target.GetWorldCorners(targetWorldCorners);
            ValidateAxisAlignment(parentRect, drivenWorldCorners, "driven");
            ValidateAxisAlignment(parentRect, targetWorldCorners, "target");
        }

        private  void ValidateAxisAlignment(RectTransform coordinateSpace, Vector3[] worldCorners, string rectRole)
        {
            Vector2 bottomLeft = coordinateSpace.InverseTransformPoint(worldCorners[0]);
            Vector2 topLeft = coordinateSpace.InverseTransformPoint(worldCorners[1]);
            Vector2 topRight = coordinateSpace.InverseTransformPoint(worldCorners[2]);
            Vector2 bottomRight = coordinateSpace.InverseTransformPoint(worldCorners[3]);
            bool axisAligned = Mathf.Approximately(bottomLeft.x, topLeft.x) && Mathf.Approximately(topLeft.y, topRight.y) && Mathf.Approximately(topRight.x, bottomRight.x) && Mathf.Approximately(bottomRight.y, bottomLeft.y);
            bool forwardFacing = bottomLeft.x <= topRight.x && bottomLeft.y <= topRight.y;
            if (!axisAligned || !forwardFacing) throw new InvalidOperationException($"{nameof(RectTransformEdgeBleed)} requires the {rectRole} RectTransform to be axis-aligned and non-mirrored in the driven RectTransform parent's coordinate space.");
        }
    }
}
