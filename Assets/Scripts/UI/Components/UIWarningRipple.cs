using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class UIWarningRipple : MonoBehaviour
    {
        [SerializeField] private Image rippleTemplate;
        [SerializeField, Min(1)] private int rippleCount = 2;
        [SerializeField, Min(0f)] private float rippleDuration = 1.5f;
        [SerializeField, Range(0f, 1f)] private float startingScale = 0.05f;
        [SerializeField, Min(1f)] private float endingScale = 1.5f;
        [SerializeField] private AnimationCurve fadeCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.75f, 0f), new Keyframe(1f, 1f));

        private readonly List<Image> ripples = new List<Image>();
        private readonly List<Vector3> restingScales = new List<Vector3>();
        private readonly List<Color> visibleColors = new List<Color>();
        private readonly List<Sequence> sequences = new List<Sequence>();
        private bool isPlaying;

        private void Awake()
        {
            if (rippleCount < 1)
                throw new ArgumentOutOfRangeException(nameof(rippleCount), rippleCount, "Ripple count must be at least one.");

            ripples.Add(rippleTemplate);
            for (int index = 1; index < rippleCount; index++)
            {
                Image ripple = Instantiate(rippleTemplate, rippleTemplate.transform.parent);
                ripple.name = $"{rippleTemplate.name} {index + 1}";
                ripples.Add(ripple);
            }

            foreach (Image ripple in ripples)
            {
                Vector3 restingScale = ripple.rectTransform.localScale;
                restingScales.Add(restingScale);
                visibleColors.Add(ripple.color);
                HideRipple(ripple, restingScale, startingScale);
            }
        }

        public void Play()
        {
            if (isPlaying) return;

            isPlaying = true;
            for (int index = 0; index < ripples.Count; index++)
            {
                float delay = rippleDuration * index / ripples.Count;
                sequences.Add(CreateSequence(ripples[index], restingScales[index], visibleColors[index], delay));
            }
        }

        public void Stop()
        {
            foreach (Sequence sequence in sequences)
                sequence.Kill();

            sequences.Clear();
            isPlaying = false;

            for (int index = 0; index < ripples.Count; index++)
                HideRipple(ripples[index], restingScales[index], startingScale);
        }

        private Sequence CreateSequence(Image ripple, Vector3 restingScale, Color visibleColor, float delay)
        {
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.AppendCallback(() => ShowRipple(ripple, restingScale, visibleColor, startingScale));
            sequence.Append(ripple.rectTransform.DOScale(restingScale * endingScale, rippleDuration).SetEase(Ease.OutQuad));
            sequence.Join(ripple.DOFade(0f, rippleDuration).SetEase(fadeCurve));
            sequence.SetLoops(-1, LoopType.Restart);
            if (delay > 0f) sequence.SetDelay(delay, false);
            return sequence;
        }

        private static void ShowRipple(Image ripple, Vector3 restingScale, Color visibleColor, float startingScale)
        {
            ripple.rectTransform.localScale = restingScale * startingScale;
            ripple.color = visibleColor;
        }

        private static void HideRipple(Image ripple, Vector3 restingScale, float startingScale)
        {
            ripple.rectTransform.localScale = restingScale * startingScale;
            Color hiddenColor = ripple.color;
            hiddenColor.a = 0f;
            ripple.color = hiddenColor;
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
