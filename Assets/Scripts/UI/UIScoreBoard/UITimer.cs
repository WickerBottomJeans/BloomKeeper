using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UITimer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        public void Display(ConstrainerViewData viewData)
        {
            gameObject.SetActive(true);
            if (label == null) return;
            label.text = viewData.constrainerText;
        }

        public void Clear()
        {
            gameObject.SetActive(false);
            if (label == null) return;
            label.text = string.Empty;
        }
    }
}
