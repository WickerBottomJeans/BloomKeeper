using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class StarBoard : MonoBehaviour
    {
        [SerializeField] private Transform starRoot;
        [SerializeField] private StarToggle starTemplate;

        private readonly List<StarToggle> stars = new();
        private int currentStars = -1;

        public void Init(int starCap)
        {
            ClearStars();
            currentStars = -1;
            starTemplate.gameObject.SetActive(false);

            for (int i = 0; i < starCap; i++)
            {
                StarToggle star = Instantiate(starTemplate, starRoot);
                ;
                star.gameObject.SetActive(true);
                star.SetImmediate(false);
                stars.Add(star);
            }
        }

        public void DisplayStars(int currentStars, float duration = 0f)
        {
            if (this.currentStars == currentStars) return;

            int previousStars = this.currentStars;
            this.currentStars = currentStars;
            int changedStars = CountChangedStars(previousStars, currentStars);
            int changedStarIndex = 0;

            for (int i = 0; i < stars.Count; i++)
            {
                bool wasOn = i < previousStars;
                bool isOn = i < currentStars;
                if (wasOn == isOn) continue;

                float delay = GetStarDelay(changedStarIndex, changedStars, duration, stars[i].ToggleAnimationTime);
                stars[i].SetOn(isOn, delay);
                changedStarIndex++;
            }
        }

        private int CountChangedStars(int previousStars, int currentStars)
        {
            int changedStars = 0;
            for (int i = 0; i < stars.Count; i++)
            {
                bool wasOn = i < previousStars;
                bool isOn = i < currentStars;
                if (wasOn != isOn)
                    changedStars++;
            }

            return changedStars;
        }

        private float GetStarDelay(int changedStarIndex, int changedStars, float duration, float starAnimationTime)
        {
            if (duration <= 0f || changedStars <= 1) return 0f;

            float staggerWindow = Mathf.Max(0f, duration - starAnimationTime);
            return staggerWindow * changedStarIndex / (changedStars - 1);
        }

        private void ClearStars()
        {
            foreach (StarToggle star in stars)
            {
                if (star != null)
                    Destroy(star.gameObject);
            }

            stars.Clear();
        }
    }
}
