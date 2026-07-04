using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private Image starsImage;
        [SerializeField] private Sprite[] starSprites;
        [SerializeField] private Button button;

        private int levelId;
        private Action<int> onSelected;

        public void Init(LevelMeta meta, Action<int> onSelected)
        {
            levelId = meta.levelId;
            this.onSelected = onSelected;
            levelNameText.text = meta.levelName;
            int earnedStars = PlayerProgress.Instance.GetStars(meta.levelId);
            starsImage.sprite = starSprites[earnedStars];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            onSelected?.Invoke(levelId);
        }
    }
}
