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
                Destroy(gameBoardInstance.gameObject);

            gameBoardInstance = Instantiate(gameBoardPrefab);
            gameBoardInstance.Init(grid);
        }

        public void HideGameBoard()
        {
            gameBoardInstance?.gameObject.SetActive(false);
        }
    }
}