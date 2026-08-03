using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private StarBoard starBoard;
        [SerializeField] private Button button;

        private int levelId;
        private Action<int> onSelected;

        public void Init(ChapterLevelDisplayData meta, int earnedStars, int starCap, bool isUnlocked, Action<int> onSelected)
        {
            levelId = meta.levelId;
            this.onSelected = onSelected;
            levelNameText.text = meta.levelName;
            starBoard.SetStarCap(starCap);
            starBoard.DisplayImmediate(earnedStars);
            starBoard.gameObject.SetActive(isUnlocked);
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            if (isUnlocked)
                button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            onSelected?.Invoke(levelId);
        }
    }
}
