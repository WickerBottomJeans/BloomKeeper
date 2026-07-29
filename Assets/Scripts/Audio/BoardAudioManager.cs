using System;
using UnityEngine;

namespace DefaultNamespace.Audio
{
    public sealed class BoardAudioManager : MonoBehaviour
    {
        [SerializeField] private AudioCue petalSwapCue;
        [SerializeField] private AudioCue invalidSwapCue;
        [SerializeField] private AudioCue matchClearCue;
        [SerializeField] private AudioCue petalLandingCue;
        [SerializeField] private AudioCue petalSpawningCue;
        [SerializeField] private AudioCue boardShuffleCue;
        [SerializeField] private AudioCue spiderWebClearCue;
        [SerializeField] private AudioCue stripedSkillCue;
        [SerializeField] private AudioCue butterflyPrepareCue;
        [SerializeField] private AudioCue butterflyFinishCue;

        public void PlayPetalSwap()
        {
            AudioService.Instance.PlaySfx(petalSwapCue);
        }

        public void PlayInvalidSwap()
        {
            AudioService.Instance.PlaySfx(invalidSwapCue);
        }

        public void PlayMatchClear()
        {
            AudioService.Instance.PlaySfx(matchClearCue);
        }

        public void PlayPetalLanding()
        {
            AudioService.Instance.PlaySfx(petalLandingCue);
        }

        public void PlayPetalSpawning()
        {
            AudioService.Instance.PlaySfx(petalSpawningCue);
        }

        public void PlayBoardShuffle()
        {
            AudioService.Instance.PlaySfx(boardShuffleCue);
        }

        public void PlayStripedSkill()
        {
            AudioService.Instance.PlaySfx(stripedSkillCue);
        }

        public void PlayButterflyPrepare()
        {
            AudioService.Instance.PlaySfx(butterflyPrepareCue);
        }

        public void PlayButterflyFinish()
        {
            AudioService.Instance.PlaySfx(butterflyFinishCue);
        }

        public void PlayObstacleCleared(TileType tileType)
        {
            AudioCue cue = tileType switch
            {
                TileType.Web => spiderWebClearCue,
                _ => throw new ArgumentOutOfRangeException(nameof(tileType), tileType, "Obstacle clear audio is not configured for this tile type.")
            };

            AudioService.Instance.PlaySfx(cue);
        }
    }
}
