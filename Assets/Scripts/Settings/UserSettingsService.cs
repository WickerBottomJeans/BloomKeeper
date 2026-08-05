using System;
using DefaultNamespace.Utility;
using UnityEngine;

namespace DefaultNamespace.Settings
{
    public sealed class UserSettingsService : Singleton<UserSettingsService>
    {
        private float musicVolume;
        private float sfxVolume;
        private bool isDirty;
        private bool ownsSettings;

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;

        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            musicVolume = PlayerPrefsStore.LoadMusicVolume();
            sfxVolume = PlayerPrefsStore.LoadSfxVolume();
            ownsSettings = true;
        }

        public void SetMusicVolume(float value)
        {
            ValidateVolume(value);
            if (Mathf.Approximately(musicVolume, value)) return;

            musicVolume = value;
            isDirty = true;
            MusicVolumeChanged?.Invoke(value);
        }

        public void SetSfxVolume(float value)
        {
            ValidateVolume(value);
            if (Mathf.Approximately(sfxVolume, value)) return;

            sfxVolume = value;
            isDirty = true;
            SfxVolumeChanged?.Invoke(value);
        }

        public void Commit()
        {
            if (!isDirty) return;

            PlayerPrefsStore.SaveAudioSettings(musicVolume, sfxVolume);
            isDirty = false;
        }

        private static void ValidateVolume(float value)
        {
            if (value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Volume must be between 0 and 1.");
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (ownsSettings && isPaused)
                Commit();
        }

        private void OnApplicationQuit()
        {
            if (ownsSettings)
                Commit();
        }
    }
}
