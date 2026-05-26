using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
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

        public void Init(LevelMeta meta)
        {
            levelId = meta.levelId;
            levelNameText.text = meta.levelName;
            int earnedStars = PlayerProgress.Instance.GetStars(meta.levelId);
            starsImage.sprite = starSprites[earnedStars];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            LevelManager.Instance.InitNewLevel(levelId);
            UIManager.Instance.HideLevelSelect();
        }
    }
}