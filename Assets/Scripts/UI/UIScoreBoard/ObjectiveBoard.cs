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

        private readonly List<ObjectiveWidget> spawnedWidgets = new();

        public void Display(List<ObjectiveViewData> viewData)
        {
            Clear();

            Transform parent = objectiveWidgetsRoot != null ? objectiveWidgetsRoot : transform;
            List<RectTransform> itemRects = new();

            foreach (ObjectiveViewData data in viewData)
            {
                ObjectiveWidget widget = Instantiate(widgetPrefab, parent);
                widget.Display(data);
                widget.gameObject.SetActive(true);

                spawnedWidgets.Add(widget);

                if (widget.transform is RectTransform itemRect)
                    itemRects.Add(itemRect);
            }

            separatedChildrenView?.SetItems(itemRects);
        }

        public void Refresh(List<ObjectiveViewData> viewData)
        {
            if (viewData.Count != spawnedWidgets.Count)
                throw new InvalidOperationException($"Objective view data count changed during the level. Expected {spawnedWidgets.Count}, received {viewData.Count}.");

            bool hasProgress = false;
            bool hasCompletion = false;

            for (int index = 0; index < spawnedWidgets.Count; index++)
            {
                ObjectiveUpdateState updateState = spawnedWidgets[index].PresentUpdate(viewData[index]);
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

            foreach (ObjectiveWidget widget in spawnedWidgets)
            {
                if (widget != null)
                    Destroy(widget.gameObject);
            }
            spawnedWidgets.Clear();
        }
    }
}
