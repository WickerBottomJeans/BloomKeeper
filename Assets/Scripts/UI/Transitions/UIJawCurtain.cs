using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class UIJawCurtain : MonoBehaviour
    {
        [SerializeField] private RectTransform upperJaw;
        [SerializeField] private RectTransform lowerJaw;
        [SerializeField] private TipBoard tipBoard;
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private Ease closeEase = Ease.InOutCubic;
        [SerializeField] private Ease openEase = Ease.InOutCubic;
        [SerializeField, Range(0f, 1f)] private float closedMeetingAnchorY = 0.5f;
        private Sequence sequence;
#if UNITY_EDITOR
        private bool isTestOpen;
#endif

#if UNITY_EDITOR
        private void Update()
        {
            // UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            // if (keyboard == null || !keyboard.pKey.wasPressedThisFrame) return;
            //
            // if (isTestOpen)
            //     Close();
            // else
            //     Open();
            //
            // isTestOpen = !isTestOpen;
        }
#endif

        public UniTask Close(string tipText = "", Sprite tipSprite = null)
        {
            KillSequence();
            SetTip(tipText, tipSprite);

            GetClosedPositions(out float upperY, out float lowerY);
            sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Join(upperJaw.DOAnchorPosY(upperY, duration).SetEase(closeEase));
            sequence.Join(lowerJaw.DOAnchorPosY(lowerY, duration).SetEase(closeEase));
            return PlaySequence(sequence);
        }

        public UniTask Open()
        {
            KillSequence();

            GetOpenPositions(out float upperY, out float lowerY);
            sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Join(upperJaw.DOAnchorPosY(upperY, duration).SetEase(openEase));
            sequence.Join(lowerJaw.DOAnchorPosY(lowerY, duration).SetEase(openEase));
            return PlaySequence(sequence);
        }

        public void SnapOpen()
        {
            KillSequence();
            GetOpenPositions(out float upperY, out float lowerY);
            upperJaw.anchoredPosition = new Vector2(upperJaw.anchoredPosition.x, upperY);
            lowerJaw.anchoredPosition = new Vector2(lowerJaw.anchoredPosition.x, lowerY);
        }

        public void SnapClosed(string tipText, Sprite tipSprite = null)
        {
            KillSequence();
            SetTip(tipText, tipSprite);
            GetClosedPositions(out float upperY, out float lowerY);
            upperJaw.anchoredPosition = new Vector2(upperJaw.anchoredPosition.x, upperY);
            lowerJaw.anchoredPosition = new Vector2(lowerJaw.anchoredPosition.x, lowerY);
        }

        private void SetTip(string tipText, Sprite tipSprite)
        {
            tipBoard.SetTip(tipText, tipSprite);
        }

        private void GetOpenPositions(out float upperY, out float lowerY)
        {
            RectTransform root = (RectTransform)transform;
            upperY = GetAnchoredYForEdge(root, upperJaw, root.rect.yMax, false);
            lowerY = GetAnchoredYForEdge(root, lowerJaw, root.rect.yMin, true);
        }

        private void GetClosedPositions(out float upperY, out float lowerY)
        {
            RectTransform root = (RectTransform)transform;
            float meetingY = Mathf.Lerp(root.rect.yMin, root.rect.yMax, closedMeetingAnchorY);
            upperY = GetAnchoredYForEdge(root, upperJaw, meetingY, false);
            lowerY = GetAnchoredYForEdge(root, lowerJaw, meetingY, true);
        }

        private float GetAnchoredYForEdge(RectTransform root, RectTransform rect, float targetY, bool topEdge)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, rect);
            float currentY = topEdge ? bounds.max.y : bounds.min.y;
            return rect.anchoredPosition.y + targetY - currentY;
        }

        private void KillSequence()
        {
            if (sequence == null) return;

            sequence.Kill();
            sequence = null;
        }

        private async UniTask PlaySequence(Sequence activeSequence)
        {
            try
            {
                await activeSequence.ToUniTask();
            }
            finally
            {
                if (sequence == activeSequence)
                    sequence = null;
            }
        }
    }
}
