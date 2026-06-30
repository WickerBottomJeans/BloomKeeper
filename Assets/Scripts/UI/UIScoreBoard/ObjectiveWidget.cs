    using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ObjectiveWidget : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI label;

        public void Display(ObjectiveViewData viewData)
        {
            label.text = viewData.objectiveText;
            icon.sprite = SpriteLoader.Instance.GetSprite(viewData.spriteKey);
        }
    }
}