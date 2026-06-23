using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

public class UIScoreBoard : MonoBehaviour
{
    [SerializeField] private RectTransform objectiveWidgetsRoot;
    [SerializeField] private ObjectiveWidget widgetPrefab;
    [SerializeField] private UIMoveCounter moveCounter;
    [SerializeField] private UITimer timer;

    private readonly List<(ObjectiveWidget widget, Func<ObjectiveViewData> getData)> spawnedWidgets = new();

    public void Init(List<IObjective> objectives, List<ConstrainerViewData> constrainerViewData)
    {
        ClearWidgets();
        ClearConstrainers();

        Transform parent = objectiveWidgetsRoot != null ? objectiveWidgetsRoot : transform;

        foreach (IObjective objective in objectives)
        {
            List<ObjectiveViewData> viewDataList = objective.GetViewData();
            for (int i = 0; i < viewDataList.Count; i++)
            {
                int capturedIndex = i;
                IObjective capturedObjective = objective;

                ObjectiveWidget widget = Instantiate(widgetPrefab, parent);
                widget.Display(viewDataList[capturedIndex]);

                spawnedWidgets.Add((widget, () => capturedObjective.GetViewData()[capturedIndex]));
            }
        }

        DisplayConstrainers(constrainerViewData);
    }

    public void RefreshObjectives()
    {
        foreach (var (widget, getData) in spawnedWidgets)
            widget.Display(getData());
    }

    public void DisplayConstrainers(List<ConstrainerViewData> viewData)
    {
        foreach (ConstrainerViewData data in viewData)
        {
            switch (data.constrainerType)
            {
                case ConstrainerType.MoveLimit:
                    moveCounter?.Display(data);
                    break;
                case ConstrainerType.TimeLimit:
                    timer?.Display(data);
                    break;
            }
        }
    }

    private void ClearWidgets()
    {
        foreach (var (widget, _) in spawnedWidgets)
        {
            if (widget != null)
                Destroy(widget.gameObject);
        }
        spawnedWidgets.Clear();
    }

    private void ClearConstrainers()
    {
        moveCounter?.Clear();
        timer?.Clear();
    }

    public float GetHeightInWorldUnits()
    {
        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        return corners[1].y - corners[0].y;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
