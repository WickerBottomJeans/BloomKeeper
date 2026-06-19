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

    public static async UniTask PlayPetalChange(
        PetalView view,
        Petal petal,
        float cellSize,
        float duration)
    {
        await view.transform.DOScale(Vector3.zero, duration * 0.4f)
            .SetEase(Ease.InBack)
            .ToUniTask();

        view.Init(petal, cellSize);
        view.transform.localScale = Vector3.zero;

        await view.transform.DOScale(view.TargetScale, duration * 0.6f)
            .SetEase(Ease.OutBack)
            .ToUniTask();
    }
    
    public static UniTask PlayComboMerge(PetalView viewA, PetalView viewB)
    {
        Vector3 midpoint = (viewA.transform.position + viewB.transform.position) / 2f;

        Sequence seq = DOTween.Sequence();

        // Stand up - quick anticipation bounce
        seq.Append(viewA.transform.DOScale(viewA.TargetScale * 1.2f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(viewB.transform.DOScale(viewB.TargetScale * 1.2f, 0.15f).SetEase(Ease.OutQuad));

        // Fly to each other
        seq.Append(viewA.transform.DOMove(midpoint, 0.25f).SetEase(Ease.InOutQuad));
        seq.Join(viewB.transform.DOMove(midpoint, 0.25f).SetEase(Ease.InOutQuad));

        return seq.ToUniTask();
    }

    public static UniTask PlayComboSpinAndDisappear(
        PetalView viewA,
        PetalView viewB,
        float duration)
    {
        Sequence seq = DOTween.Sequence();
        float spinDuration = duration * (5f / 7f);
        float disappearDuration = duration * (2f / 7f);

        // Spin together in place, opposite directions
        seq.Append(viewA.transform.DORotate(new Vector3(0f, 0f, 720f), spinDuration, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
        seq.Join(viewB.transform.DORotate(new Vector3(0f, 0f, -720f), spinDuration, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));

        // Burst away
        seq.Append(viewA.transform.DOScale(Vector3.zero, disappearDuration).SetEase(Ease.InBack));
        seq.Join(viewA.spriteRenderer.DOFade(0f, disappearDuration));
        seq.Join(viewB.transform.DOScale(Vector3.zero, disappearDuration).SetEase(Ease.InBack));
        seq.Join(viewB.spriteRenderer.DOFade(0f, disappearDuration));

        return seq.ToUniTask();
    }
}
