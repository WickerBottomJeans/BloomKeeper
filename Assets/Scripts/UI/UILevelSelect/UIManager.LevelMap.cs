namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        public const string Address_UILevelSelect = "UILevelSelect";
        private UILevelSelect levelSelectInstance;

        public async void ShowLevelSelect()
        {
            if (levelSelectInstance == null)
                levelSelectInstance = await LoadPanel<UILevelSelect>(Address_UILevelSelect);
            levelSelectInstance.gameObject.SetActive(true);
        }

        public void HideLevelSelect()
        {
            levelSelectInstance?.gameObject.SetActive(false);
        }
    }
}