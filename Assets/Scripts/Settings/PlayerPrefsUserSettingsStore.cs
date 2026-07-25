using System;
using UnityEngine;

namespace DefaultNamespace.Settings
{
    sealed class PlayerPrefsUserSettingsStore
    {
        private const string MusicVolumeKey = "Settings.Audio.MusicVolume";
        private const string SfxVolumeKey = "Settings.Audio.SfxVolume";

        public float LoadMusicVolume()
        {
            return LoadVolume(MusicVolumeKey);
        }

        public float LoadSfxVolume()
        {
            return LoadVolume(SfxVolumeKey);
        }

        public void Save(float musicVolume, float sfxVolume)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
        }

        private static float LoadVolume(string key)
        {
            float volume = PlayerPrefs.GetFloat(key, 1f);
            if (volume < 0f || volume > 1f)
                throw new InvalidOperationException($"Stored volume '{key}' must be between 0 and 1, but was {volume}.");

            return volume;
        }
    }
}
