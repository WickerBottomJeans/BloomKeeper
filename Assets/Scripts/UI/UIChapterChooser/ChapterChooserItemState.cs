namespace DefaultNamespace.UI
{
    public class ChapterChooserItemState
    {
        public ChapterIndexEntry Chapter { get; }
        public bool IsCurrent { get; }
        public bool IsUnlocked { get; }

        public ChapterChooserItemState(ChapterIndexEntry chapter, bool isCurrent, bool isUnlocked)
        {
            Chapter = chapter;
            IsCurrent = isCurrent;
            IsUnlocked = isUnlocked;
        }
    }
}
