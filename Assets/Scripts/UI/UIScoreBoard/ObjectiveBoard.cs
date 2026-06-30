using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class ObjectiveBoard : MonoBehaviour
    {
        [SerializeField] private RectTransform objectiveWidgetsRoot;
        [SerializeField] private ObjectiveWidget widgetPrefab;
        [SerializeField] private SeparatedChildrenView separatedChildrenView;

        private readonly List<(ObjectiveWidget widget, Func<ObjectiveViewData> getData)> spawnedWidgets = new();

        public void Display(List<IObjective> objectives)
        {
            Clear();

            Transform parent = objectiveWidgetsRoot != null ? objectiveWidgetsRoot : transform;
            List<RectTransform> itemRects = new();

            foreach (IObjective objective in objectives)
            {
                List<ObjectiveViewData> viewDataList = objective.GetViewData();
                for (int i = 0; i < viewDataList.Count; i++)
                {
                    int capturedIndex = i;
                    IObjective capturedObjective = objective;

                    ObjectiveWidget widget = Instantiate(widgetPrefab, parent);
                    widget.Display(viewDataList[capturedIndex]);
                    widget.gameObject.SetActive(true);

                    spawnedWidgets.Add((widget, () => capturedObjective.GetViewData()[capturedIndex]));

                    if (widget.transform is RectTransform itemRect)
                        itemRects.Add(itemRect);
                }
            }

            separatedChildrenView?.SetItems(itemRects);
        }

        public void Refresh()
        {
            foreach (var (widget, getData) in spawnedWidgets)
                widget.Display(getData());
        }

        public void Clear()
        {
            separatedChildrenView?.ClearSeparators();

            foreach (var (widget, _) in spawnedWidgets)
            {
                if (widget != null)
                    Destroy(widget.gameObject);
            }
            spawnedWidgets.Clear();
        }
    }
}
