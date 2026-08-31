using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Shows the app startup visual before account entry is ready.
    /// </summary>
    public class UIStartupScreen : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private CanvasGroup bootVisualGroup;
        [SerializeField] private CanvasGroup accountEntryVisualGroup;
        [SerializeField] private Image bootLoadingImage;
        [SerializeField] private List<Sprite> bootLoadingSprites;
        [SerializeField, Min(0f)] private float stateTransitionDuration = 0.2f;
        [SerializeField, Min(0f)] private float bootLoadingRotationDuration = 5f;

        private StartupScreenState? currentState;
        private Vector3 bootLoadingBaseEulerAngles;
        private Sequence stateTransition;
        private Tween bootLoadingRotationTween;

        public event Action PlayRequested;

        #region Unity Lifecycle

        private void Awake()
        {
            bootLoadingBaseEulerAngles = bootLoadingImage.rectTransform.localEulerAngles;
            playButton.onClick.AddListener(HandlePlayClicked);
        }

        private void OnDisable()
        {
            stateTransition?.Kill();
            stateTransition = null;
            StopBootLoadingRotation();
            currentState = null;
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
            stateTransition?.Kill();
            bootLoadingRotationTween?.Kill();
        }

        #endregion

        #region Public API

        public void Show(StartupScreenState state)
        {
            ChangeState(state);
        }

        public void ChangeState(StartupScreenState state)
        {
            if (!currentState.HasValue)
            {
                ApplyStateImmediately(state);
                currentState = state;
                return;
            }

            if (currentState.Value == state) return;

            stateTransition?.Kill();
            ApplyStateImmediately(currentState.Value);
            currentState = state;

            if (state == StartupScreenState.Boot)
                TransitionToBoot();
            else
                TransitionToAccountEntry();
        }

        #endregion

        #region Private Methods

        private void HandlePlayClicked()
        {
            PlayRequested?.Invoke();
        }

        private void ApplyStateImmediately(StartupScreenState state)
        {
            if (state == StartupScreenState.Boot)
            {
                ShowBootVisual();
                HideAccountEntryVisual();
                return;
            }

            HideBootVisual();
            ShowAccountEntryVisual();
        }

        private void TransitionToBoot()
        {
            bootVisualGroup.gameObject.SetActive(true);
            bootVisualGroup.alpha = 0f;
            bootVisualGroup.interactable = false;
            bootVisualGroup.blocksRaycasts = false;
            SelectRandomBootLoadingSprite();
            StartBootLoadingRotation();

            accountEntryVisualGroup.interactable = false;
            accountEntryVisualGroup.blocksRaycasts = false;
            stateTransition = DOTween.Sequence()
                .Join(accountEntryVisualGroup.DOFade(0f, stateTransitionDuration))
                .Join(bootVisualGroup.DOFade(1f, stateTransitionDuration))
                .OnComplete(CompleteBootTransition);
        }

        private void TransitionToAccountEntry()
        {
            accountEntryVisualGroup.gameObject.SetActive(true);
            accountEntryVisualGroup.alpha = 0f;
            accountEntryVisualGroup.interactable = false;
            accountEntryVisualGroup.blocksRaycasts = false;

            stateTransition = DOTween.Sequence()
                .Join(bootVisualGroup.DOFade(0f, stateTransitionDuration))
                .Join(accountEntryVisualGroup.DOFade(1f, stateTransitionDuration))
                .OnComplete(CompleteAccountEntryTransition);
        }

        private void CompleteBootTransition()
        {
            HideAccountEntryVisual();
            stateTransition = null;
        }

        private void CompleteAccountEntryTransition()
        {
            HideBootVisual();
            ShowAccountEntryVisual();
            stateTransition = null;
        }

        private void ShowBootVisual()
        {
            bootVisualGroup.gameObject.SetActive(true);
            bootVisualGroup.alpha = 1f;
            bootVisualGroup.interactable = false;
            bootVisualGroup.blocksRaycasts = false;
            SelectRandomBootLoadingSprite();
            StartBootLoadingRotation();
        }

        private void HideBootVisual()
        {
            StopBootLoadingRotation();
            bootVisualGroup.gameObject.SetActive(false);
        }

        private void ShowAccountEntryVisual()
        {
            accountEntryVisualGroup.gameObject.SetActive(true);
            accountEntryVisualGroup.alpha = 1f;
            accountEntryVisualGroup.interactable = true;
            accountEntryVisualGroup.blocksRaycasts = true;
        }

        private void HideAccountEntryVisual()
        {
            accountEntryVisualGroup.gameObject.SetActive(false);
            accountEntryVisualGroup.interactable = false;
            accountEntryVisualGroup.blocksRaycasts = false;
        }

        private void SelectRandomBootLoadingSprite()
        {
            bootLoadingImage.sprite = bootLoadingSprites[UnityEngine.Random.Range(0, bootLoadingSprites.Count)];
        }

        private void StartBootLoadingRotation()
        {
            bootLoadingRotationTween?.Kill();
            bootLoadingImage.rectTransform.localEulerAngles = bootLoadingBaseEulerAngles;
            bootLoadingRotationTween = bootLoadingImage.rectTransform.DOLocalRotate(bootLoadingBaseEulerAngles + new Vector3(0f, 0f, -360f), bootLoadingRotationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopBootLoadingRotation()
        {
            bootLoadingRotationTween?.Kill();
            bootLoadingRotationTween = null;
            bootLoadingImage.rectTransform.localEulerAngles = bootLoadingBaseEulerAngles;
        }

        #endregion
    }
}
