using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(menuName = "BloomKeeper/Tutorials/Home Tutorial Config")]
    public class HomeTutorialConfig : ScriptableObject
    {
        [SerializeField, TextArea] private string livesMessage = "Use a life to enter a level and get it back when you win. Lives refill over time, and unlimited lives let you play without using them.";
        [SerializeField, TextArea] private string diamondsMessage = "These are your diamonds. Earn them by playing and use them in the shop.";
        [SerializeField, TextArea] private string mapMessage = "Tap Map to see your chapters.";
        [SerializeField, TextArea] private string chaptersMessage = "Your adventure is split into chapters. Keep completing levels to unlock more chapters.";
        [SerializeField, TextArea] private string closeChaptersMessage = "Close the chapter chooser. Let's visit the shop next.";
        [SerializeField, TextArea] private string shopMessage = "Tap Shop to take a look.";
        [SerializeField, TextArea] private string shopExplanationMessage = "Browse the shop for helpful items. Each offer shows what you get and its price.";
        [SerializeField] private string continueLabel = "Next";
        [SerializeField] private string doneLabel = "Done";
        [SerializeField] private string saveFailureTitle = "Introduction progress";
        [SerializeField, TextArea] private string saveRetryMessage = "We couldn't confirm that your introduction was saved. Retry, or cancel to continue. The introduction may appear again if you cancel.";
        [SerializeField, TextArea] private string saveFailureMessage = "We couldn't confirm that your introduction was saved. You can continue playing, but the introduction may appear again.";

        public string LivesMessage => livesMessage;
        public string DiamondsMessage => diamondsMessage;
        public string MapMessage => mapMessage;
        public string ChaptersMessage => chaptersMessage;
        public string CloseChaptersMessage => closeChaptersMessage;
        public string ShopMessage => shopMessage;
        public string ShopExplanationMessage => shopExplanationMessage;
        public string ContinueLabel => continueLabel;
        public string DoneLabel => doneLabel;
        public string SaveFailureTitle => saveFailureTitle;
        public string SaveRetryMessage => saveRetryMessage;
        public string SaveFailureMessage => saveFailureMessage;
    }
}
