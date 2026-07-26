using DefaultNamespace.Audio;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UITimer : MonoBehaviour, IWarningView
    {
        [SerializeField] private UIJellyText label;
        [SerializeField] private UIWarningRipple warningRipple;
        [SerializeField] private UIShake warningShake;
        [SerializeField] private AudioCue warningCue;

        private bool isWarningActive;

        public void Display(ConstrainerViewData viewData)
        {
            gameObject.SetActive(true);
            label.SetText(viewData.constrainerText);
            SetWarningActive(viewData.isWarning);
        }

        public void SetWarningActive(bool isActive)
        {
            if (isWarningActive == isActive) return;

            isWarningActive = isActive;
            if (!isActive)
            {
                warningRipple.Stop();
                return;
            }

            warningRipple.Play();
            warningShake.Play();
            AudioService.Instance.PlaySfx(warningCue);
        }

        public void Clear()
        {
            label.Clear();
            SetWarningActive(false);
            gameObject.SetActive(false);
        }
    }
}
