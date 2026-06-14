using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public static class PetalViewAnimator
{
    public static UniTask PlaySwap(PetalView view, Vector2 targetPos)
    {
        return view.transform.DOMove(targetPos, 0.2f)
            .SetEase(Ease.OutQuad)
            .ToUniTask();
    }

    public static UniTask PlayDestroy(PetalView view, float delay = 0f)
    {
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay);
        seq.Append(view.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        seq.Join(view.spriteRenderer.DOFade(0f, 0.2f));
        return seq.ToUniTask();
    }

    public static UniTask PlaySpawn(PetalView view)
    {
        view.transform.localScale = Vector3.zero;
        view.spriteRenderer.color = new Color(
            view.spriteRenderer.color.r,
            view.spriteRenderer.color.g,
            view.spriteRenderer.color.b, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(view.transform.DOScale(view.TargetScale, 0.2f).SetEase(Ease.OutBack));
        seq.Join(view.spriteRenderer.DOFade(1f, 0.2f));
        return seq.ToUniTask();
    }

    public static UniTask PlayDrop(PetalView view, Vector2 targetPos)
    {
        return view.transform.DOMove(targetPos, 0.25f)
            .SetEase(Ease.InQuad)
            .ToUniTask();
    }
}