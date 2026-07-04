using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIWinScreen : MonoBehaviour
    {
        [SerializeField] private Button homeButton;

        public event Action HomeRequested;

        private void Awake()
        {
            homeButton.onClick.AddListener(HandleHomeClicked);
        }

        private void HandleHomeClicked()
        {
            HomeRequested?.Invoke();
        }
    }
}
