namespace DefaultNamespace
{
    /// <summary>
    /// Icon key and prepared text for one shop grant.
    /// </summary>
    public class ShopGrantDisplayData
    {
        public string PresentationKey { get; }
        public string DisplayText { get; }

        public ShopGrantDisplayData(string presentationKey, string displayText)
        {
            PresentationKey = presentationKey;
            DisplayText = displayText;
        }
    }
}
