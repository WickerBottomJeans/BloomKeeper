using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace DefaultNamespace.Audio
{
    class SfxPlayer : IDisposable
    {
        private readonly Transform voiceRoot;
        private readonly List<AudioVoice> activeVoices = new();
        private readonly ObjectPool<AudioVoice> voicePool;
        private bool isGameplayPaused;

        public SfxPlayer(Transform voiceRoot)
        {
            this.voiceRoot = voiceRoot != null ? voiceRoot : throw new ArgumentNullException(nameof(voiceRoot));
            voicePool = new ObjectPool<AudioVoice>(CreateVoice, actionOnRelease: voice => voice.Reset(), actionOnDestroy: voice => voice.Destroy());
        }

        public void Play(AudioCue cue, AudioMixerGroup mixerGroup)
        {
            AudioClip clip;
            try
            {
                clip = cue.PickClip();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception, cue);
                return;
            }

            float pitch = cue.PickPitch();
            AudioVoice voice = voicePool.Get();

            try
            {
                voice.Play(clip, mixerGroup, cue.Bus, cue.Volume, pitch);
                if (cue.Bus == AudioBus.GameplaySfx && isGameplayPaused)
                    voice.Pause();
                activeVoices.Add(voice);
            }
            catch
            {
                voicePool.Release(voice);
                throw;
            }
        }

        public void Tick()
        {
            for (int index = activeVoices.Count - 1; index >= 0; index--)
            {
                AudioVoice voice = activeVoices[index];
                if (!voice.HasFinished) continue;

                activeVoices.RemoveAt(index);
                voicePool.Release(voice);
            }
        }

        public void SetGameplayPaused(bool isPaused)
        {
            isGameplayPaused = isPaused;

            foreach (AudioVoice voice in activeVoices)
            {
                if (voice.Bus != AudioBus.GameplaySfx) continue;

                if (isPaused && !voice.IsPaused)
                    voice.Pause();
                else if (!isPaused && voice.IsPaused)
                    voice.Resume();
            }
        }

        public void Dispose()
        {
            for (int index = activeVoices.Count - 1; index >= 0; index--)
                voicePool.Release(activeVoices[index]);

            activeVoices.Clear();
            voicePool.Clear();
        }

        private AudioVoice CreateVoice()
        {
            GameObject voiceObject = new GameObject("SFX Voice");
            voiceObject.transform.SetParent(voiceRoot);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return new AudioVoice(source);
        }
    }
}
