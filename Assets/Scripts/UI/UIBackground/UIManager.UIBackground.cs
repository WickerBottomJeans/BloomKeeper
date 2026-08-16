using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIBackground backgroundPrefab;
        private UIBackground backgroundInstance;

        public void ShowBackground(Texture backgroundTexture)
        {
            GetPanel(ref backgroundInstance, backgroundPrefab, uiRoot);
            backgroundInstance.Show(backgroundTexture);
        }

        public void HideBackground()
        {
            if (backgroundInstance == null) return;

            backgroundInstance.Hide();
        }
    }
}
