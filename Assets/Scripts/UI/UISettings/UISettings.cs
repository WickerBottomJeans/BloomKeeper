using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class UISettings : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button backButton;

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action CloseRequested;

        private void Awake()
        {
            musicSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            backButton.onClick.AddListener(HandleBackClicked);
        }

        public void Show(float musicVolume, float sfxVolume)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            sfxSlider.SetValueWithoutNotify(sfxVolume);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleMusicVolumeChanged(float value)
        {
            MusicVolumeChanged?.Invoke(value);
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
        }
    }
}
