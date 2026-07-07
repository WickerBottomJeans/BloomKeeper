using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public static class PetalViewAnimator
{
    public static UniTask PlaySwap(PetalView view, Vector2 targetPos, float cellSize)
    {
        return PlayJellyishMove(view, targetPos, cellSize, 0.2f, Ease.OutQuad);
    }

    public static UniTask PlayDestroy(PetalView view, float delay = 0f)
    {
        view.KillVisualAnimation();
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(delay);
        seq.Append(view.VisualTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        seq.Join(view.spriteRenderer.DOFade(0f, 0.2f));
        return seq.ToUniTask();
    }

    public static UniTask PlayDisappear(PetalView view, float duration)
    {
        view.KillVisualAnimation();
        return view.VisualTransform.DOScale(Vector3.zero, duration)
            .SetEase(Ease.InBack)
            .ToUniTask();
    }

    public static UniTask PlayAboutToExecute(PetalView view)
    {
        view.KillVisualAnimation();
        view.UseAboutToExecuteMaterial();
        view.VisualTransform.localScale = view.TargetScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(view.VisualTransform.DOScale(view.TargetScale * 1.08f, 0.18f).SetEase(Ease.InOutSine));
        seq.Append(view.VisualTransform.DOScale(view.TargetScale * 0.94f, 0.18f).SetEase(Ease.InOutSine));
        seq.Append(view.VisualTransform.DOScale(view.TargetScale, 0.18f).SetEase(Ease.InOutSine));
        seq.SetTarget(view.VisualTransform);
        seq.SetLoops(-1, LoopType.Restart);

        return UniTask.CompletedTask;
    }

    public static UniTask PlayAboutToExecute(PetalView view, float duration) => PlayAboutToExecute(view);

    public static UniTask PlaySpawn(PetalView view)
    {
        view.KillVisualAnimation();
        view.VisualTransform.localScale = Vector3.zero;
        view.spriteRenderer.color = new Color(
            view.spriteRenderer.color.r,
            view.spriteRenderer.color.g,
            view.spriteRenderer.color.b, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(view.VisualTransform.DOScale(view.TargetScale, 0.2f).SetEase(Ease.OutBack));
        seq.Join(view.spriteRenderer.DOFade(1f, 0.2f));
        return seq.ToUniTask();
    }

    public static UniTask PlayDrop(PetalView view, Vector2 targetPos, float cellSize)
    {
        return PlayJellyishMove(view, targetPos, cellSize, 0.25f, Ease.InQuad);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="view"></param>
    /// <param name="targetPos"></param>
    /// <param name="cellSize"></param>
    /// <param name="duration">Root movement time</param>
    /// <param name="moveEase"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static UniTask PlayJellyishMove(PetalView view, Vector2 targetPos, float cellSize, float duration, Ease moveEase)
    {
        if (cellSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cellSize),
                "Directional jelly movement requires a positive cell size.");
        if (duration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(duration),
                "Directional jelly movement requires a positive duration.");

        view.KillRootAnimation();
        view.StretchAxis.DOKill();
        view.ResetDirectionalJelly();

        Vector2 startPos = view.transform.position;
        Vector2 displacement = targetPos - startPos;

        //If the petal basically isnt moving
        if (displacement.sqrMagnitude <= Mathf.Epsilon)
            return view.transform.DOMove(targetPos, duration).SetEase(moveEase).ToUniTask();

        PrepareDirectionalJelly(view, displacement, cellSize, duration, out Vector3 stretchScale,
            out Vector3 squashScale);

        float squashDuration = duration * view.DirectionalJellySquashDurationRatio;
        float settleDuration = duration * view.DirectionalJellySettleDurationRatio;

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(view.transform);
        seq.Join(view.transform.DOMove(targetPos, duration).SetEase(moveEase));
        seq.Join(view.StretchAxis.DOScale(stretchScale, duration).SetEase(Ease.OutQuad));
        seq.Append(view.StretchAxis.DOScale(squashScale, squashDuration).SetEase(Ease.InOutSine));
        seq.Append(view.StretchAxis.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutBack));
        seq.OnKill(view.ResetDirectionalJelly);
        return seq.ToUniTask();
    }

    private static void PrepareDirectionalJelly(PetalView view, Vector2 displacement, float cellSize, float duration, out Vector3 stretchScale, out Vector3 squashScale)
    {
        float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;
        float speedCellsPerSecond = displacement.magnitude / cellSize / duration;
        float stretchAmount = Mathf.Clamp(speedCellsPerSecond * view.DirectionalJellyStrength, 0f, view.MaxDirectionalJellyStretch);

        view.StretchAxis.localRotation = Quaternion.Euler(0f, 0f, angle);
        view.VisualTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

        stretchScale = new Vector3(1f + stretchAmount, 1f - stretchAmount, 1f);
        squashScale = new Vector3(1f - stretchAmount, 1f + stretchAmount, 1f);
    }   

    public static async UniTask PlayFly(PetalView view, Vector2 targetWorldPosition, float duration)
    {
        view.KillActiveAnimation();
        Vector2 flightDirection = targetWorldPosition - (Vector2)view.transform.position;
        float targetAngle = Vector2.SignedAngle(Vector2.up, flightDirection);

        Tween flapTween = view.VisualTransform.DOScaleX(0f, 0.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

        try
        {
            await UniTask.WhenAll(view.transform.DOMove(targetWorldPosition, duration).SetEase(Ease.Linear).ToUniTask(),
                view.VisualTransform.DORotate(new Vector3(0f, 0f, targetAngle), duration, RotateMode.Fast)
                    .SetEase(Ease.OutQuad).ToUniTask());
        }
        finally
        {
            flapTween.Kill();
            view.VisualTransform.localScale = view.TargetScale;
        }
    }

    public static async UniTask PlayPetalChange(PetalView view, Petal petal, float cellSize, float duration)
    {
        view.KillVisualAnimation();
        await view.VisualTransform.DOScale(Vector3.zero, duration * 0.4f)
            .SetEase(Ease.InBack)
            .ToUniTask();

        view.Init(petal, cellSize);
        view.VisualTransform.localScale = Vector3.zero;

        await view.VisualTransform.DOScale(view.TargetScale, duration * 0.6f)
            .SetEase(Ease.OutBack)
            .ToUniTask();
    }

    public static UniTask PlayComboMerge(PetalView viewA, PetalView viewB)
    {
        viewA.KillActiveAnimation();
        viewB.KillActiveAnimation();
        Vector3 midpoint = (viewA.transform.position + viewB.transform.position) / 2f;

        Sequence seq = DOTween.Sequence();

        // Stand up - quick anticipation bounce
        seq.Append(viewA.VisualTransform.DOScale(viewA.TargetScale * 1.2f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(viewB.VisualTransform.DOScale(viewB.TargetScale * 1.2f, 0.15f).SetEase(Ease.OutQuad));

        // Fly to each other
        seq.Append(viewA.transform.DOMove(midpoint, 0.25f).SetEase(Ease.InOutQuad));
        seq.Join(viewB.transform.DOMove(midpoint, 0.25f).SetEase(Ease.InOutQuad));

        return seq.ToUniTask();
    }

    public static UniTask PlayComboSpinAndDisappear(PetalView viewA, PetalView viewB, float duration)
    {
        viewA.KillVisualAnimation();
        viewB.KillVisualAnimation();
        Sequence seq = DOTween.Sequence();
        float spinDuration = duration * (5f / 7f);
        float disappearDuration = duration * (2f / 7f);

        // Spin together in place, opposite directions
        seq.Append(viewA.VisualTransform.DORotate(new Vector3(0f, 0f, 720f), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad));
        seq.Join(viewB.VisualTransform.DORotate(new Vector3(0f, 0f, -720f), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad));

        // Burst away
        seq.Append(viewA.VisualTransform.DOScale(Vector3.zero, disappearDuration).SetEase(Ease.InBack));
        seq.Join(viewA.spriteRenderer.DOFade(0f, disappearDuration));
        seq.Join(viewB.VisualTransform.DOScale(Vector3.zero, disappearDuration).SetEase(Ease.InBack));
        seq.Join(viewB.spriteRenderer.DOFade(0f, disappearDuration));

        return seq.ToUniTask();
    }
}
