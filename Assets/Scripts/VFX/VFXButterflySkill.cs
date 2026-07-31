using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace.VFX
{
    public class VFXButterflySkill : MonoBehaviour
    {
        [FormerlySerializedAs("butterfly")]
        [SerializeField] private SpriteRenderer butterflyGlowingOutline;
        [SerializeField] private ParticleSystem fireParticle;
        [SerializeField] private TrailRenderer leftWingTrail;
        [SerializeField] private TrailRenderer rightWingTrail;
        [SerializeField] private ParticleSystem impactParticles;
        [SerializeField] private AudioCue prepareCue;
        [SerializeField] private AudioCue finishCue;

        private Color restingColor;

        private void Awake()
        {
            restingColor = butterflyGlowingOutline.color;
        }

        public void SetColor(Color color)
        {
            ParticleSystem.MainModule fireMain = fireParticle.main;
            ParticleSystem.MainModule impactMain = impactParticles.main;
            fireMain.startColor = color;
            impactMain.startColor = color;
            SetTrailColor(leftWingTrail, color);
            SetTrailColor(rightWingTrail, color);
        }

        public async UniTask Prepare(float duration, AudioPlaybackScope audioScope)
        {
            AudioService.Instance.PlaySfx(prepareCue, audioScope);
            fireParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            leftWingTrail.emitting = false;
            rightWingTrail.emitting = false;
            leftWingTrail.Clear();
            rightWingTrail.Clear();
            butterflyGlowingOutline.color = new Color(restingColor.r, restingColor.g, restingColor.b, 0f);

            await butterflyGlowingOutline.DOFade(restingColor.a, duration).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        public void Fire()
        {
            fireParticle.Play();
            leftWingTrail.emitting = true;
            rightWingTrail.emitting = true;
        }

        public async UniTask Finish(float duration, AudioPlaybackScope audioScope)
        {
            AudioService.Instance.PlaySfx(finishCue, audioScope);
            fireParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            leftWingTrail.emitting = false;
            rightWingTrail.emitting = false;
            impactParticles.Play();
            await butterflyGlowingOutline.DOFade(0f, duration).SetLink(gameObject, LinkBehaviour.KillOnDestroy).ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, this.GetCancellationTokenOnDestroy());
        }

        private static void SetTrailColor(TrailRenderer trail, Color color)
        {
            Gradient gradient = trail.colorGradient;
            GradientColorKey[] colorKeys = gradient.colorKeys;

            for (int i = 0; i < colorKeys.Length; i++)
                colorKeys[i].color = new Color(color.r, color.g, color.b, colorKeys[i].color.a);

            gradient.SetKeys(colorKeys, gradient.alphaKeys);
            trail.colorGradient = gradient;
        }
    }
}
