using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;
using UnityEngine.UI;

public class UILevelHud : MonoBehaviour
{
    [SerializeField] private ObjectiveBoard objectiveBoard;
    [SerializeField] private UIMoveCounter moveCounter;
    [SerializeField] private UITimer timer;
    [SerializeField] private UIScoreBoard scoreBoard;
    [SerializeField] private Button pauseButton;

    public event Action PauseRequested;

    private void Awake()
    {
        pauseButton.onClick.AddListener(HandlePauseClicked);
    }

    public void Init(IReadOnlyList<ObjectiveViewData> objectiveViewData, IReadOnlyList<ConstrainerViewData> constrainerViewData, ScoreViewData scoreViewData)
    {
        ClearConstrainers();
        objectiveBoard.Display(objectiveViewData);
        DisplayConstrainers(constrainerViewData);
        scoreBoard.Init(scoreViewData.TargetScore, scoreViewData.MilestoneScores, scoreViewData.StarCap);
    }

    public void RefreshObjectives(List<ObjectiveViewData> objectiveViewData)
    {
        objectiveBoard.Refresh(objectiveViewData);
    }

    public void DisplayConstrainers(IReadOnlyList<ConstrainerViewData> viewData)
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

    public void InitScoreBoard(int targetScore, IReadOnlyList<int> milestoneScores, int starCap)
    {
        scoreBoard.Init(targetScore, milestoneScores, starCap);
    }

    public void DisplayScore(int score, int stars)
    {
        scoreBoard.DisplayScore(score, stars);
    }

    public float GetHeightInWorldUnits()
    {
        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        return corners[1].y - corners[0].y;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    private void HandlePauseClicked()
    {
        PauseRequested?.Invoke();
    }
}
