using System;
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
        private int starCap = -1;

        public void SetStarCap(int starCap)
        {
            if (starCap < 0) throw new ArgumentOutOfRangeException(nameof(starCap), starCap, "Star cap cannot be negative.");
            if (this.starCap == starCap) return;

            ClearStars();
            this.starCap = starCap;
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

        public void DisplayImmediate(int currentStars)
        {
            ValidateStarCount(currentStars);
            this.currentStars = currentStars;
            for (int i = 0; i < stars.Count; i++) stars[i].SetImmediate(i < currentStars);
        }

        public void DisplayAnimated(int currentStars, float duration = 0f)
        {
            ValidateStarCount(currentStars);
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

        private void ValidateStarCount(int currentStars)
        {
            if (starCap < 0) throw new InvalidOperationException("Set the star cap before displaying stars.");
            if (currentStars < 0 || currentStars > starCap) throw new ArgumentOutOfRangeException(nameof(currentStars), currentStars, $"Displayed stars must be between 0 and the configured cap of {starCap}.");
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
