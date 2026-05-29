using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIGameBoard gameBoardPrefab;
        private UIGameBoard gameBoardInstance;

        public void ShowGameBoard(Tile[,] grid)
        {
            if (gameBoardInstance != null)
            {
                gameBoardInstance.OnPetalsCleared -= HandlePetalsCleared;
                Destroy(gameBoardInstance.gameObject);
            }

            gameBoardInstance = Instantiate(gameBoardPrefab);
            gameBoardInstance.Init(grid);
            gameBoardInstance.OnPetalsCleared += HandlePetalsCleared;
        }

        private void HandlePetalsCleared(List<PetalType> clearedPetals)
        {
            LevelManager.Instance.ReportCleared(clearedPetals);
        }

        public void HideGameBoard()
        {
            gameBoardInstance?.gameObject.SetActive(false);
        }
    }
}