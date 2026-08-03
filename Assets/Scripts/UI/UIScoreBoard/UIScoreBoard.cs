using System.Collections.Generic;
using DefaultNamespace.UI;
using TMPro;
using UnityEngine;

public class UIScoreBoard : MonoBehaviour
{
    [SerializeField] private UIProgressionBar progressionBar;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private StarBoard starBoard;

    public void Init(int targetScore, IReadOnlyList<int> milestoneScores, int starCap)
    {
        progressionBar?.Init(targetScore, milestoneScores);
        starBoard?.SetStarCap(starCap);
        DisplayScore(0, 0);
    }

    public void DisplayScore(int score, int stars)
    {
        progressionBar?.DisplayValue(score);
        starBoard?.DisplayAnimated(stars);
        scoreLabel.text = score.ToString();
    }
}
