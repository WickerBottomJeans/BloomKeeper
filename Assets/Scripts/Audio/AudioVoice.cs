using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DefaultNamespace.Audio
{
    sealed class AudioVoice
    {
        private readonly AudioSource source;

        public AudioBus Bus { get; private set; }
        public bool IsPaused { get; private set; }
        public bool HasFinished => !IsPaused && !source.isPlaying;

        public AudioVoice(AudioSource source)
        {
            this.source = source != null ? source : throw new ArgumentNullException(nameof(source));
        }

        public void Play(AudioClip clip, AudioMixerGroup mixerGroup, AudioBus bus, float volume, float pitch)
        {
            Bus = bus;
            source.clip = clip;
            source.outputAudioMixerGroup = mixerGroup;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        public void Pause()
        {
            if (IsPaused)
                throw new InvalidOperationException("Cannot pause an audio voice that is already paused.");

            source.Pause();
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsPaused)
                throw new InvalidOperationException("Cannot resume an audio voice that is not paused.");

            source.UnPause();
            IsPaused = false;
        }

        public void Reset()
        {
            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.volume = 1f;
            source.pitch = 1f;
            Bus = default;
            IsPaused = false;
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(source.gameObject);
        }
    }
}
