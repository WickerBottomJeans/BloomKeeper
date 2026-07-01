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

            for (int i = 0; i < starCap; i++)
            {
                StarToggle star = Instantiate(starTemplate, starRoot);
                star.gameObject.SetActive(true);
                star.SetImmediate(false);
                stars.Add(star);
            }
        }

        public void DisplayStars(int currentStars)
        {
            if (this.currentStars == currentStars) return;

            this.currentStars = currentStars;
            for (int i = 0; i < stars.Count; i++)
                stars[i].SetOn(i < currentStars);
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
