using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

public class UILevelHud : MonoBehaviour
{
    [SerializeField] private ObjectiveBoard objectiveBoard;
    [SerializeField] private UIMoveCounter moveCounter;
    [SerializeField] private UITimer timer;

    public void Init(List<IObjective> objectives, List<ConstrainerViewData> constrainerViewData)
    {
        ClearConstrainers();
        objectiveBoard.Display(objectives);
        DisplayConstrainers(constrainerViewData);
    }

    public void RefreshObjectives()
    {
        objectiveBoard.Refresh();
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
