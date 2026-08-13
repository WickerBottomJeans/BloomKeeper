using DefaultNamespace.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UIButtonAudioFeedback : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private AudioCue buttonDownCue;
        [SerializeField] private AudioCue buttonReleaseCue;

        private void Awake()
        {
            button.onClick.AddListener(HandleClicked);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;

            AudioService.Instance.PlaySfx(buttonDownCue);
        }

        private void HandleClicked()
        {
            AudioService.Instance.PlaySfx(buttonReleaseCue);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }
}
