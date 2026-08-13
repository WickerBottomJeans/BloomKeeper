using UnityEngine;

namespace DefaultNamespace.Audio
{
    public class PlaySfxOnEnable : MonoBehaviour
    {
        [SerializeField] private AudioCue cue;

        private void OnEnable()
        {
            AudioService.Instance.PlaySfx(cue);
        }
    }
}
