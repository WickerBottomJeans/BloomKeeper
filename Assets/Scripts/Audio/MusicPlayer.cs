using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace DefaultNamespace.Audio
{
    class MusicPlayer : IDisposable
    {
        private readonly AudioMixerGroup mixerGroup;
        private readonly float crossfadeDuration;
        private AudioSource activeSource;
        private AudioSource standbySource;
        private AudioCue activeCue;
        private Sequence transition;
        private bool activeSourceWasPlayingBeforeStinger;
        private bool standbySourceWasPlayingBeforeStinger;
        private bool isPausedForStinger;

        public MusicPlayer(Transform voiceRoot, AudioMixerGroup mixerGroup, float crossfadeDuration)
        {
            if (voiceRoot == null)
                throw new ArgumentNullException(nameof(voiceRoot));
            if (crossfadeDuration < 0f)
                throw new ArgumentOutOfRangeException(nameof(crossfadeDuration), crossfadeDuration, "Music crossfade duration cannot be negative.");

            this.mixerGroup = mixerGroup;
            this.crossfadeDuration = crossfadeDuration;
            activeSource = CreateSource(voiceRoot, "Music Voice A");
            standbySource = CreateSource(voiceRoot, "Music Voice B");
        }

        public void Play(AudioCue cue)
        {
            if (activeCue == cue && activeSource.isPlaying)
                return;

            AudioClip clip = cue.PickClip();
            float pitch = cue.PickPitch();
            transition?.Kill();
            transition = null;

            AudioSource outgoingSource = activeSource;
            AudioSource incomingSource = standbySource;
            incomingSource.Stop();
            incomingSource.clip = clip;
            incomingSource.volume = 0f;
            incomingSource.pitch = pitch;
            incomingSource.Play();

            activeSource = incomingSource;
            standbySource = outgoingSource;
            activeCue = cue;

            if (crossfadeDuration == 0f)
            {
                outgoingSource.Stop();
                incomingSource.volume = cue.Volume;
                return;
            }

            transition = DOTween.Sequence().SetUpdate(true);
            if (outgoingSource.isPlaying)
                transition.Join(outgoingSource.DOFade(0f, crossfadeDuration).OnComplete(outgoingSource.Stop));
            transition.Join(incomingSource.DOFade(cue.Volume, crossfadeDuration));
            transition.OnComplete(() => transition = null);
        }

        public void PauseForStinger()
        {
            if (isPausedForStinger)
                throw new InvalidOperationException("Music is already paused for a stinger.");

            activeSourceWasPlayingBeforeStinger = activeSource.isPlaying;
            standbySourceWasPlayingBeforeStinger = standbySource.isPlaying;
            transition?.Pause();

            if (activeSourceWasPlayingBeforeStinger)
                activeSource.Pause();
            if (standbySourceWasPlayingBeforeStinger)
                standbySource.Pause();

            isPausedForStinger = true;
        }

        public void ResumeAfterStinger()
        {
            if (!isPausedForStinger)
                throw new InvalidOperationException("Music cannot resume because it is not paused for a stinger.");

            if (activeSourceWasPlayingBeforeStinger)
                activeSource.UnPause();
            if (standbySourceWasPlayingBeforeStinger)
                standbySource.UnPause();

            transition?.Play();
            activeSourceWasPlayingBeforeStinger = false;
            standbySourceWasPlayingBeforeStinger = false;
            isPausedForStinger = false;
        }

        public void Dispose()
        {
            transition?.Kill();
            transition = null;
            activeSource.Stop();
            standbySource.Stop();
            UnityEngine.Object.Destroy(activeSource.gameObject);
            UnityEngine.Object.Destroy(standbySource.gameObject);
        }

        private AudioSource CreateSource(Transform voiceRoot, string sourceName)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(voiceRoot);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }
    }
}
