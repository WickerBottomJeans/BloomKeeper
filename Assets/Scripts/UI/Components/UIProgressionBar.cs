using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIProgressionBar : MonoBehaviour
    {
        [SerializeField] private RectMask2D fillMask;
        [SerializeField] private RectTransform milestoneRoot;
        [SerializeField] private RectTransform milestoneTemplate;

        private readonly List<RectTransform> milestoneInstances = new();
        private int targetValue;

        public void Init(int targetValue, IReadOnlyList<int> milestoneValues)
        {
            this.targetValue = targetValue;
            DisplayValue(0);
            ClearMilestones();

            if (milestoneTemplate == null) return;
            if (milestoneRoot == null) return;

            foreach (int milestoneValue in milestoneValues)
            {
                RectTransform milestone = Instantiate(milestoneTemplate, milestoneRoot);
                milestone.gameObject.SetActive(true);
                SetAnchorX(milestone, GetProgress(milestoneValue));
                milestoneInstances.Add(milestone);
            }
        }

        public void DisplayValue(int currentValue)
        {
            if (fillMask == null) return;

            RectTransform maskRect = fillMask.transform as RectTransform;
            if (maskRect == null) return;

            float hiddenWidth = maskRect.rect.width * (1f - GetProgress(currentValue));
            fillMask.padding = new Vector4(0f, 0f, hiddenWidth, 0f);
        }

        private float GetProgress(int value)
        {
            if (targetValue <= 0) return 1f;
            return Mathf.Clamp01((float)value / targetValue);
        }

        private static void SetAnchorX(RectTransform rect, float x)
        {
            rect.anchorMin = new Vector2(x, rect.anchorMin.y);
            rect.anchorMax = new Vector2(x, rect.anchorMax.y);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        }

        private void ClearMilestones()
        {
            foreach (RectTransform milestone in milestoneInstances)
            {
                if (milestone != null)
                    Destroy(milestone.gameObject);
            }

            milestoneInstances.Clear();
        }
    }
}
