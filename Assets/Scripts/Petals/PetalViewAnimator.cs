using DG.Tweening;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PetalViewAnimator
{
    public static void PlaySwap(PetalView view, Vector2 targetPos, Action onComplete = null)
    {
        view.transform.DOMove(targetPos, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void PlayDestroy(PetalView view, float delay = 0f, Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay);
        seq.Append(view.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        seq.Join(view.spriteRenderer.DOFade(0f, 0.2f));
        seq.OnComplete(() => onComplete?.Invoke());
    }
    
    public static void PlaySpawn(PetalView view, Action onComplete = null)
    {
        view.transform.localScale = Vector3.zero;
        view.spriteRenderer.color = new Color(view.spriteRenderer.color.r, view.spriteRenderer.color.g, view.spriteRenderer.color.b, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(view.transform.DOScale(view.TargetScale, 0.2f).SetEase(Ease.OutBack));
        seq.Join(view.spriteRenderer.DOFade(1f, 0.2f));
        seq.OnComplete(() => onComplete?.Invoke());
    }
    
    public static void PlayDrop(PetalView view, Vector2 targetPos, Action onComplete = null)
    {
        view.transform.DOMove(targetPos, 0.25f)
            .SetEase(Ease.InQuad)
            .OnComplete(() => onComplete?.Invoke());
    }
}