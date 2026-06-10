using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIScoreBoard scoreBoardPrefab;
        private UIScoreBoard scoreBoardInstance;

        public void ShowScoreBoard(List<IObjective> objectives)
        {
            if (scoreBoardInstance == null)
                scoreBoardInstance = Instantiate(scoreBoardPrefab, canvas.transform);
            scoreBoardInstance.Init(objectives);
            scoreBoardInstance.Show();
        }

        public void HideScoreBoard()
        {
            scoreBoardInstance?.Hide();
        }

        public float GetScoreBoardHeightInWorldUnit()
        {
            float result = 0;
            if (this.scoreBoardInstance != null)
            {
                result = scoreBoardInstance.GetHeightInWorldUnits();
            }
            return result;
        }

        public void RefreshObjectiveOnScoreBoard()
        {
            if (scoreBoardInstance == null)
            {
                return;
            }
            scoreBoardInstance.Refresh();
        }
    }
}