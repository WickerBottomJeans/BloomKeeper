using DefaultNamespace;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ObjectiveWidget : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField, Min(1f)] private float progressBounceScale = 1.15f;
        [SerializeField, Min(0f)] private float progressBounceDuration = 0.3f;
        [SerializeField, Range(0f, 1f)] private float completedAlpha = 0.45f;

        private Sequence updateSequence;
        private Vector3 restingScale;
        private int displayedRemainingAmount;

        private void Awake()
        {
            restingScale = transform.localScale;
        }

        public void Display(ObjectiveViewData viewData)
        {
            label.text = viewData.objectiveText;
            icon.sprite = SpriteLoader.Instance.GetSprite(viewData.spriteKey);
            SetContentAlpha(viewData.remainingAmount <= 0 ? completedAlpha : 1f);
            displayedRemainingAmount = viewData.remainingAmount;
        }

        public ObjectiveUpdateState PresentUpdate(ObjectiveViewData viewData)
        {
            int previousRemainingAmount = displayedRemainingAmount;
            ObjectiveUpdateState updateState = viewData.remainingAmount >= previousRemainingAmount
                ? ObjectiveUpdateState.NoChange
                : viewData.remainingAmount == 0
                    ? ObjectiveUpdateState.Completed
                    : ObjectiveUpdateState.Progressed;

            Display(viewData);
            if (updateState == ObjectiveUpdateState.NoChange) return updateState;

            updateSequence?.Kill();
            transform.localScale = restingScale;
            
            bool completed = updateState == ObjectiveUpdateState.Completed;
            if (completed)
                SetContentAlpha(1f);

            float halfDuration = progressBounceDuration * 0.5f;
            updateSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            updateSequence.Append(transform.DOScale(restingScale * progressBounceScale, halfDuration).SetEase(Ease.OutQuad));
            updateSequence.Append(transform.DOScale(restingScale, halfDuration).SetEase(Ease.OutBack));

            if (completed)
            {
                updateSequence.Append(icon.DOFade(completedAlpha, halfDuration));
                updateSequence.Join(label.DOFade(completedAlpha, halfDuration));
            }

            updateSequence.OnComplete(() => updateSequence = null);
            return updateState;
        }

        private void OnDisable()
        {
            updateSequence?.Kill();
            updateSequence = null;
            transform.localScale = restingScale;
        }

        private void SetContentAlpha(float alpha)
        {
            Color iconColor = icon.color;
            iconColor.a = alpha;
            icon.color = iconColor;
            label.alpha = alpha;
        }
    }
}
