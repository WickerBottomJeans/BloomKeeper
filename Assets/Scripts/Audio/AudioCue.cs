using System;
using UnityEngine;

namespace DefaultNamespace.Audio
{
    [CreateAssetMenu(fileName = "AudioCue", menuName = "BloomKeeper/Audio Cue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioBus bus;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float minimumPitch = 1f;
        [SerializeField] private float maximumPitch = 1f;

        public AudioBus Bus => bus;
        public float Volume => volume;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
                throw new InvalidOperationException($"Audio cue '{name}' does not contain any clips.");

            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip == null)
                throw new InvalidOperationException($"Audio cue '{name}' contains an empty clip reference.");

            return clip;
        }

        public float PickPitch()
        {
            if (minimumPitch > maximumPitch)
                throw new InvalidOperationException($"Audio cue '{name}' has a minimum pitch greater than its maximum pitch.");

            return UnityEngine.Random.Range(minimumPitch, maximumPitch);
        }
    }
}
