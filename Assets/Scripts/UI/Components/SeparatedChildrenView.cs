using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class SeparatedChildrenView : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform separatorPrefab;

        private readonly List<RectTransform> separators = new();

        public void SetItems(IReadOnlyList<RectTransform> items)
        {
            ClearSeparators();

            RectTransform root = contentRoot != null ? contentRoot : transform as RectTransform;
            if (root == null || items == null)
                return;

            if (root.GetComponent<LayoutGroup>() == null)
            {
                Debug.LogError($"{nameof(SeparatedChildrenView)} requires a LayoutGroup on its content root.", this);
                return;
            }

            int validItemCount = CountValidItems(items);
            int placedItemCount = 0;
            int siblingIndex = 0;

            foreach (RectTransform item in items)
            {
                if (item == null)
                {
                    Debug.LogError($"{nameof(SeparatedChildrenView)} received a null item.", this);
                    continue;
                }

                item.SetParent(root, false);
                item.SetSiblingIndex(siblingIndex);
                siblingIndex++;
                placedItemCount++;

                if (separatorPrefab == null || placedItemCount >= validItemCount)
                    continue;

                RectTransform separator = Instantiate(separatorPrefab, root);
                separator.gameObject.SetActive(true);
                separator.SetSiblingIndex(siblingIndex);
                siblingIndex++;
                separators.Add(separator);
            }
        }

        public void ClearSeparators()
        {
            foreach (RectTransform separator in separators)
            {
                if (separator != null)
                    Destroy(separator.gameObject);
            }
            separators.Clear();
        }

        private int CountValidItems(IReadOnlyList<RectTransform> items)
        {
            int count = 0;
            foreach (RectTransform item in items)
            {
                if (item != null)
                    count++;
            }
            return count;
        }
    }
}
