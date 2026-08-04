using UnityEngine;

namespace DefaultNamespace.UI
{
    public sealed class UIFriendsView : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
