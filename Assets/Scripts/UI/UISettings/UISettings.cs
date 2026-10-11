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
        [SerializeField] private Button shareAppButton;

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action CloseRequested;
        public event Action ReviewRequested;
        public event Action ShareAppRequested;

        protected override void Awake()
        {
            base.Awake();
            musicSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            backButton.onClick.AddListener(HandleBackClicked);
            rateAppButton.onClick.AddListener(HandleRateAppClicked);
            shareAppButton.onClick.AddListener(HandleShareAppClicked);
        }

        public void Show(float musicVolume, float sfxVolume, bool isGooglePlayReviewSupported, bool isAppSharingSupported)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            sfxSlider.SetValueWithoutNotify(sfxVolume);
            rateAppButton.gameObject.SetActive(isGooglePlayReviewSupported);
            shareAppButton.gameObject.SetActive(isAppSharingSupported);
            base.Show();
        }

        public void SetReviewPending(bool isReviewPending)
        {
            rateAppButton.interactable = !isReviewPending;
            shareAppButton.interactable = !isReviewPending;
            backButton.interactable = !isReviewPending;
        }

        public void SetSharePending(bool isSharePending)
        {
            rateAppButton.interactable = !isSharePending;
            shareAppButton.interactable = !isSharePending;
            backButton.interactable = !isSharePending;
        }

        private void HandleShareAppClicked()
        {
            ShareAppRequested?.Invoke();
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
            shareAppButton.onClick.RemoveListener(HandleShareAppClicked);
        }
    }
}
