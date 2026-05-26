using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIScoreBoard scoreBoardPrefab;
        private UIScoreBoard scoreBoardInstance;

        public void ShowScoreBoard()
        {
            if (scoreBoardInstance == null)
                scoreBoardInstance = Instantiate(scoreBoardPrefab, canvas.transform);
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
    }
}