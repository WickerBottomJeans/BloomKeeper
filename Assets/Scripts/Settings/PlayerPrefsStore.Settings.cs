using System;
using UnityEngine;

namespace DefaultNamespace.Settings
{
    public static partial class PlayerPrefsStore
    {
        private const string MusicVolumeKey = "Settings.Audio.MusicVolume";
        private const string SfxVolumeKey = "Settings.Audio.SfxVolume";

        public static float LoadMusicVolume()
        {
            return LoadVolume(MusicVolumeKey);
        }

        public static float LoadSfxVolume()
        {
            return LoadVolume(SfxVolumeKey);
        }

        public static void SaveAudioSettings(float musicVolume, float sfxVolume)
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
