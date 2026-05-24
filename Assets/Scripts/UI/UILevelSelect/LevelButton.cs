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
        
        public void Init(LevelMeta meta)
        {
            levelNameText.text = meta.levelName;
            int earnedStars = PlayerProgress.Instance.GetStars(meta.levelId);
            starsImage.sprite = starSprites[earnedStars];
        }
    }
}