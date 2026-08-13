using UnityEngine;

namespace DefaultNamespace.Audio
{
    public class MusicStateController : MonoBehaviour
    {
        [SerializeField] private AudioCue homeMusicCue;
        [SerializeField] private AudioCue gameplayMusicCue;

        public void EnterHome()
        {
            AudioService.Instance.PlayMusic(homeMusicCue);
        }

        public void EnterLevel()
        {
            AudioService.Instance.PlayMusic(gameplayMusicCue);
        }
    }
}
