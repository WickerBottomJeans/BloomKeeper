using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Audio;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class ObjectiveBoard : MonoBehaviour
    {
        [SerializeField] private RectTransform objectiveWidgetsRoot;
        [SerializeField] private ObjectiveWidget widgetPrefab;
        [SerializeField] private SeparatedChildrenView separatedChildrenView;
        [SerializeField] private AudioCue objectiveProgressCue;
        [SerializeField] private AudioCue objectiveCompleteCue;

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
            bool hasProgress = false;
            bool hasCompletion = false;

            foreach (var (widget, getData) in spawnedWidgets)
            {
                ObjectiveUpdateState updateState = widget.PresentUpdate(getData());
                if (updateState == ObjectiveUpdateState.Completed)
                    hasCompletion = true;
                else if (updateState == ObjectiveUpdateState.Progressed)
                    hasProgress = true;
            }

            if (hasCompletion)
                AudioService.Instance.PlaySfx(objectiveCompleteCue);
            else if (hasProgress)
                AudioService.Instance.PlaySfx(objectiveProgressCue);
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
