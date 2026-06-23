using TMPro;
using UnityEngine;

namespace UI
{
    public class UILoseScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        public void Display(string message)
        {
            if (label == null) return;
            label.text = message;
        }
    }
}
