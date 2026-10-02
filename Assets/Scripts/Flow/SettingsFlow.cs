using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Reviews;
using DefaultNamespace.Settings;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class SettingsFlow
    {
        private readonly GooglePlayReviewService googlePlayReviewService = new();
        private bool isActive;
        private bool isReviewPending;

        public void Open()
        {
            if (isActive)
                throw new InvalidOperationException("Cannot enter Settings while the Settings flow is already active.");

            isActive = true;
            UIManager.Instance.SettingsMusicVolumeChanged += HandleMusicVolumeChanged;
            UIManager.Instance.SettingsSfxVolumeChanged += HandleSfxVolumeChanged;
            UIManager.Instance.SettingsCloseRequested += HandleCloseRequested;
            UIManager.Instance.SettingsReviewRequested += HandleReviewRequested;

            try
            {
                UserSettingsService settings = UserSettingsService.Instance;
                UIManager.Instance.ShowSettings(settings.MusicVolume, settings.SfxVolume, googlePlayReviewService.IsGooglePlayReviewSupported);
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
            if (isReviewPending) return;

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
            UIManager.Instance.SettingsReviewRequested -= HandleReviewRequested;
        }

        private void HandleReviewRequested()
        {
            if (isReviewPending) return;

            ApplicationOperationRunner.Instance.Run(RequestAppReviewAsync);
        }

        private async UniTask RequestAppReviewAsync()
        {
            isReviewPending = true;
            try
            {
                UIManager.Instance.SetSettingsReviewPending(true);
                if (!googlePlayReviewService.IsGooglePlayReviewSupported)
                {
                    await DialogManager.Instance.RunOkDialog("Review unavailable", "Reviews aren't available on this device.");
                    return;
                }

                bool didReviewOperationComplete = await googlePlayReviewService.TryRequestAppReviewAsync();
                if (!didReviewOperationComplete)
                    await DialogManager.Instance.RunOkDialog("Review unavailable", "We couldn't open Google Play reviews. Please try again later.");
            }
            finally
            {
                isReviewPending = false;
                UIManager.Instance.SetSettingsReviewPending(false);
            }
        }
    }
}
