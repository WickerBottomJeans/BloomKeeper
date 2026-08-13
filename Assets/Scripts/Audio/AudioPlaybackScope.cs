using System;
using System.Collections.Generic;

namespace DefaultNamespace.Audio
{
    public class AudioPlaybackScope : IDisposable
    {
        private readonly Dictionary<AudioCue, int> acceptedPlayCounts = new();
        private bool isDisposed;

        public bool TryConsume(AudioCue cue)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(AudioPlaybackScope));
            if (cue == null)
                throw new ArgumentNullException(nameof(cue));

            int maximumPlays = cue.MaximumPlaysPerScope;
            if (maximumPlays == 0)
                return true;

            acceptedPlayCounts.TryGetValue(cue, out int acceptedPlayCount);
            if (acceptedPlayCount >= maximumPlays)
                return false;

            acceptedPlayCounts[cue] = acceptedPlayCount + 1;
            return true;
        }

        public void Dispose()
        {
            if (isDisposed) return;

            acceptedPlayCounts.Clear();
            isDisposed = true;
        }
    }
}
