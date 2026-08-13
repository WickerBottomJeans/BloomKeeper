using System;
using DefaultNamespace.Settings;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class SettingsFlow
    {
        private bool isActive;

        public void Open()
        {
            if (isActive)
                throw new InvalidOperationException("Cannot enter Settings while the Settings flow is already active.");

            isActive = true;
            UIManager.Instance.SettingsMusicVolumeChanged += HandleMusicVolumeChanged;
            UIManager.Instance.SettingsSfxVolumeChanged += HandleSfxVolumeChanged;
            UIManager.Instance.SettingsCloseRequested += HandleCloseRequested;

            try
            {
                UserSettingsService settings = UserSettingsService.Instance;
                UIManager.Instance.ShowSettings(settings.MusicVolume, settings.SfxVolume);
            }
            catch
            {
                Unbind();
                isActive = false;
                throw;
            }
        }

        private void HandleMusicVolumeChanged(float value)
        {
            UserSettingsService.Instance.SetMusicVolume(value);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            UserSettingsService.Instance.SetSfxVolume(value);
        }

        private void HandleCloseRequested()
        {
            UserSettingsService.Instance.Commit();
            UIManager.Instance.HideSettings();
            Unbind();
            isActive = false;
        }

        private void Unbind()
        {
            UIManager.Instance.SettingsMusicVolumeChanged -= HandleMusicVolumeChanged;
            UIManager.Instance.SettingsSfxVolumeChanged -= HandleSfxVolumeChanged;
            UIManager.Instance.SettingsCloseRequested -= HandleCloseRequested;
        }
    }
}
