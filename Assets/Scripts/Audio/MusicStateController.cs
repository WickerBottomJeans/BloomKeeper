using UnityEngine;

namespace DefaultNamespace.Audio
{
    public sealed class MusicStateController : MonoBehaviour
    {
        [SerializeField] private AudioCue homeMusicCue;
        [SerializeField] private AudioCue gameplayMusicCue;

        public void EnterHome()
        {
            AudioService.Instance.PlayMusic(homeMusicCue);
        }

        public void EnterGameplay()
        {
            AudioService.Instance.PlayMusic(gameplayMusicCue);
        }
    }
}
