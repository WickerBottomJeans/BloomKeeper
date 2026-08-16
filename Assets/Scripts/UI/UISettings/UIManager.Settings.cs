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

        public void ShowSettings(float musicVolume, float sfxVolume)
        {
            GetPanel(ref settingsInstance, settingsPrefab, uiRoot);
            UnbindSettings();
            BindSettings();
            settingsInstance.Show(musicVolume, sfxVolume);
        }

        public void HideSettings()
        {
            UnbindSettings();
            settingsInstance?.Hide();
        }

        private void BindSettings()
        {
            settingsInstance.MusicVolumeChanged += HandleSettingsMusicVolumeChanged;
            settingsInstance.SfxVolumeChanged += HandleSettingsSfxVolumeChanged;
            settingsInstance.CloseRequested += HandleSettingsCloseRequested;
        }

        private void UnbindSettings()
        {
            if (settingsInstance == null) return;

            settingsInstance.MusicVolumeChanged -= HandleSettingsMusicVolumeChanged;
            settingsInstance.SfxVolumeChanged -= HandleSettingsSfxVolumeChanged;
            settingsInstance.CloseRequested -= HandleSettingsCloseRequested;
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
