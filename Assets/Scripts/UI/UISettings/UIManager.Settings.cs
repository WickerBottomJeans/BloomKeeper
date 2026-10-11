using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UISettings settingsPrefab;
        private UISettings settingsInstance;

        public event Action<float> SettingsMusicVolumeChanged;
        public event Action<float> SettingsSfxVolumeChanged;
        public event Action SettingsCloseRequested;
        public event Action SettingsReviewRequested;
        public event Action SettingsShareAppRequested;

        public void ShowSettings(float musicVolume, float sfxVolume, bool isGooglePlayReviewSupported, bool isAppSharingSupported)
        {
            GetPanel(ref settingsInstance, settingsPrefab, uiRoot);
            UnbindSettings();
            BindSettings();
            settingsInstance.Show(musicVolume, sfxVolume, isGooglePlayReviewSupported, isAppSharingSupported);
        }

        public void HideSettings()
        {
            UnbindSettings();
            settingsInstance?.Hide();
        }

        public void SetSettingsReviewPending(bool isReviewPending)
        {
            settingsInstance.SetReviewPending(isReviewPending);
        }

        public void SetSettingsSharePending(bool isSharePending)
        {
            settingsInstance.SetSharePending(isSharePending);
        }

        private void BindSettings()
        {
            settingsInstance.MusicVolumeChanged += HandleSettingsMusicVolumeChanged;
            settingsInstance.SfxVolumeChanged += HandleSettingsSfxVolumeChanged;
            settingsInstance.CloseRequested += HandleSettingsCloseRequested;
            settingsInstance.ReviewRequested += HandleSettingsReviewRequested;
            settingsInstance.ShareAppRequested += HandleSettingsShareAppRequested;
        }

        private void UnbindSettings()
        {
            if (settingsInstance == null) return;

            settingsInstance.MusicVolumeChanged -= HandleSettingsMusicVolumeChanged;
            settingsInstance.SfxVolumeChanged -= HandleSettingsSfxVolumeChanged;
            settingsInstance.CloseRequested -= HandleSettingsCloseRequested;
            settingsInstance.ReviewRequested -= HandleSettingsReviewRequested;
            settingsInstance.ShareAppRequested -= HandleSettingsShareAppRequested;
        }

        private void HandleSettingsShareAppRequested()
        {
            SettingsShareAppRequested?.Invoke();
        }

        private void HandleSettingsReviewRequested()
        {
            SettingsReviewRequested?.Invoke();
        }

        private void HandleSettingsMusicVolumeChanged(float value)
        {
            SettingsMusicVolumeChanged?.Invoke(value);
        }

        private void HandleSettingsSfxVolumeChanged(float value)
        {
            SettingsSfxVolumeChanged?.Invoke(value);
        }

        private void HandleSettingsCloseRequested()
        {
            SettingsCloseRequested?.Invoke();
        }
    }
}
