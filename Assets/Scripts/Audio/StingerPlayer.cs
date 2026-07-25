using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DefaultNamespace.Audio
{
    sealed class StingerPlayer : IDisposable
    {
        private readonly AudioSource source;

        public bool IsActive { get; private set; }

        public StingerPlayer(Transform voiceRoot)
        {
            if (voiceRoot == null)
                throw new ArgumentNullException(nameof(voiceRoot));

            GameObject sourceObject = new GameObject("Stinger Voice");
            sourceObject.transform.SetParent(voiceRoot);
            source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public void Play(AudioCue cue, AudioMixerGroup mixerGroup)
        {
            source.Stop();
            source.clip = cue.PickClip();
            source.outputAudioMixerGroup = mixerGroup;
            source.volume = cue.Volume;
            source.pitch = cue.PickPitch();
            source.Play();
            IsActive = true;
        }

        public bool Tick()
        {
            if (!IsActive || source.isPlaying) return false;

            IsActive = false;
            return true;
        }

        public void Stop()
        {
            source.Stop();
            IsActive = false;
        }

        public void Dispose()
        {
            source.Stop();
            UnityEngine.Object.Destroy(source.gameObject);
        }
    }
}
