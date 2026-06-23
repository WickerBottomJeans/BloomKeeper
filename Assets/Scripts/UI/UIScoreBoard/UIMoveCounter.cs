using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UIMoveCounter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        public void Display(ConstrainerViewData viewData)
        {
            if (label == null) return;
            label.text = viewData.constrainerText;
        }

        public void Clear()
        {
            if (label == null) return;
            label.text = string.Empty;
        }
    }
}
