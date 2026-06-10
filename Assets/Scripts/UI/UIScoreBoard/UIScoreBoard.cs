using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

public class UIScoreBoard : MonoBehaviour
{
    [SerializeField] private RectTransform objectiveWidgetsRoot;
    [SerializeField] private ObjectiveWidget widgetPrefab;

    private readonly List<(ObjectiveWidget widget, Func<ObjectiveViewData> getData)> spawnedWidgets = new();

    public void Init(List<IObjective> objectives)
    {
        ClearWidgets();

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
    }

    public void Refresh()
    {
        foreach (var (widget, getData) in spawnedWidgets)
            widget.Display(getData());
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

    public float GetHeightInWorldUnits()
    {
        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        return corners[1].y - corners[0].y;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}