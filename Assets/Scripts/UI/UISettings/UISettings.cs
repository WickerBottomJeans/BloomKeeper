using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class UISettings : UIPopup
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button backButton;
        [SerializeField] private Button rateAppButton;

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action CloseRequested;
        public event Action ReviewRequested;

        protected override void Awake()
        {
            base.Awake();
            musicSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            backButton.onClick.AddListener(HandleBackClicked);
            rateAppButton.onClick.AddListener(HandleRateAppClicked);
        }

        public void Show(float musicVolume, float sfxVolume, bool isGooglePlayReviewSupported)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            sfxSlider.SetValueWithoutNotify(sfxVolume);
            rateAppButton.gameObject.SetActive(isGooglePlayReviewSupported);
            base.Show();
        }

        public void SetReviewPending(bool isReviewPending)
        {
            rateAppButton.interactable = !isReviewPending;
            backButton.interactable = !isReviewPending;
        }

        private void HandleMusicVolumeChanged(float value)
        {
            MusicVolumeChanged?.Invoke(value);
        }

        private void HandleRateAppClicked()
        {
            ReviewRequested?.Invoke();
        }

        private void HandleSfxVolumeChanged(float value)
        {
            SfxVolumeChanged?.Invoke(value);
        }

        private void HandleBackClicked()
        {
            CloseRequested?.Invoke();
        }

        private void OnDestroy()
        {
            musicSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            sfxSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            backButton.onClick.RemoveListener(HandleBackClicked);
            rateAppButton.onClick.RemoveListener(HandleRateAppClicked);
        }
    }
}
