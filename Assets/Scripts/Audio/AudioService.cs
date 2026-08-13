using System;
using DefaultNamespace.Settings;
using DefaultNamespace.Utility;
using UnityEngine;
using UnityEngine.Audio;

namespace DefaultNamespace.Audio
{
    public class AudioService : Singleton<AudioService>
    {
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const float MutedDecibels = -80f;

        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup gameplaySfxMixerGroup;
        [SerializeField] private AudioMixerGroup uiSfxMixerGroup;
        [SerializeField, Min(0f)] private float musicCrossfadeDuration = 1f;

        private SfxPlayer sfxPlayer;
        private MusicPlayer musicPlayer;
        private StingerPlayer stingerPlayer;
        private UserSettingsService userSettingsService;
        private bool ownsPlayers;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            sfxPlayer = new SfxPlayer(transform);
            musicPlayer = new MusicPlayer(transform, musicMixerGroup, musicCrossfadeDuration);
            stingerPlayer = new StingerPlayer(transform);
            GameTimeService.PauseStateChanged += HandlePauseStateChanged;
            ownsPlayers = true;
        }

        private void Start()
        {
            if (!ownsPlayers) return;

            userSettingsService = UserSettingsService.Instance;
            ApplyMusicVolume(userSettingsService.MusicVolume);
            ApplySfxVolume(userSettingsService.SfxVolume);
            userSettingsService.MusicVolumeChanged += ApplyMusicVolume;
            userSettingsService.SfxVolumeChanged += ApplySfxVolume;
        }

        private void Update()
        {
            if (!ownsPlayers) return;

            sfxPlayer.Tick();
            if (stingerPlayer.Tick())
                musicPlayer.ResumeAfterStinger();
        }

        public void PlaySfx(AudioCue cue)
        {
            PlaySfx(cue, null, false);
        }

        public void PlaySfx(AudioCue cue, AudioPlaybackScope scope)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));

            PlaySfx(cue, scope, true);
        }

        private void PlaySfx(AudioCue cue, AudioPlaybackScope scope, bool isScoped)
        {
            if (cue == null)
            {
                Debug.LogError("Cannot play SFX because the AudioCue reference is missing.");
                return;
            }
            if (cue.Bus == AudioBus.Music)
                throw new InvalidOperationException($"Audio cue '{cue.name}' is music and cannot be played as SFX.");
            if (isScoped && !scope.TryConsume(cue))
                return;

            AudioMixerGroup mixerGroup = GetSfxMixerGroup(cue.Bus);
            sfxPlayer.Play(cue, mixerGroup);
        }

        public void PlayMusic(AudioCue cue)
        {
            if (cue == null)
                throw new ArgumentNullException(nameof(cue));
            if (cue.Bus != AudioBus.Music)
                throw new InvalidOperationException($"Audio cue '{cue.name}' is not music.");

            if (stingerPlayer.IsActive)
            {
                stingerPlayer.Stop();
                musicPlayer.ResumeAfterStinger();
            }

            musicPlayer.Play(cue);
        }

        public void PlayStinger(AudioCue cue)
        {
            if (cue == null)
                throw new ArgumentNullException(nameof(cue));
            if (cue.Bus == AudioBus.Music)
                throw new InvalidOperationException($"Audio cue '{cue.name}' is music and cannot be played as a stinger.");

            if (!stingerPlayer.IsActive)
                musicPlayer.PauseForStinger();

            stingerPlayer.Play(cue, GetSfxMixerGroup(cue.Bus));
        }

        private AudioMixerGroup GetSfxMixerGroup(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.GameplaySfx => gameplaySfxMixerGroup,
                AudioBus.UiSfx => uiSfxMixerGroup,
                _ => throw new ArgumentOutOfRangeException(nameof(bus), bus, "Unsupported SFX audio bus.")
            };
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            sfxPlayer.SetGameplayPaused(isPaused);
        }

        private void ApplyMusicVolume(float normalizedVolume)
        {
            SetMixerVolume(MusicVolumeParameter, normalizedVolume);
        }

        private void ApplySfxVolume(float normalizedVolume)
        {
            SetMixerVolume(SfxVolumeParameter, normalizedVolume);
        }

        private void SetMixerVolume(string parameterName, float normalizedVolume)
        {
            float decibels = normalizedVolume <= 0f ? MutedDecibels : Mathf.Log10(normalizedVolume) * 20f;
            if (!musicMixerGroup.audioMixer.SetFloat(parameterName, decibels))
                throw new InvalidOperationException($"Audio Mixer parameter '{parameterName}' is missing or unavailable.");
        }

        private void OnDestroy()
        {
            if (!ownsPlayers) return;

            GameTimeService.PauseStateChanged -= HandlePauseStateChanged;
            if (userSettingsService != null)
            {
                userSettingsService.MusicVolumeChanged -= ApplyMusicVolume;
                userSettingsService.SfxVolumeChanged -= ApplySfxVolume;
            }
            sfxPlayer.Dispose();
            stingerPlayer.Dispose();
            musicPlayer.Dispose();
            ownsPlayers = false;
        }
    }
}
