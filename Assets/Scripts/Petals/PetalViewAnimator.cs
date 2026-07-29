using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public static class PetalViewAnimator
{
    public static UniTask PlaySwap(PetalView view, Vector2 targetPos, float tileSize)
    {
        return PlayJellyishMove(view, targetPos, tileSize, 0.2f, Ease.OutQuad);
    }

    public static UniTask PlayDisappear(PetalView view, float duration, Ease ease = Ease.InBack, float delay = 0f)
    {
        view.KillVisualAnimation();
        Sequence sequence = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        sequence.AppendInterval(delay);
        sequence.Append(view.VisualTransform.DOScale(Vector3.zero, duration).SetEase(ease));
        return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    public static UniTask PlayScale(PetalView view, float scaleMultiplier, float duration, Ease ease = Ease.OutBack)
    {
        view.KillVisualAnimation();
        return view.VisualTransform.DOScale(view.TargetScale * scaleMultiplier, duration)
            .SetEase(ease)
            .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    public static UniTask PlayRootScale(PetalView view, float scaleMultiplier, float duration)
    {
        view.KillRootAnimation();
        return view.transform.DOScale(view.DefaultRootScale * scaleMultiplier, duration)
            .SetEase(Ease.OutQuad)
            .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    public static UniTask PlayAboutToExecute(PetalView view)
    {
        view.KillVisualAnimation();
        view.UseAboutToExecuteMaterial();
        view.VisualTransform.localScale = view.TargetScale;

        Sequence seq = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
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

        Sequence seq = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(view.VisualTransform.DOScale(view.TargetScale, 0.2f).SetEase(Ease.OutBack));
        seq.Join(view.spriteRenderer.DOFade(1f, 0.2f));
        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    public static UniTask PlayDrop(PetalView view, Vector2 targetPos, float tileSize)
    {
        return PlayJellyishMove(view, targetPos, tileSize, 0.25f, Ease.InQuad);
    }

    public static UniTask PlayGather(PetalView view, Vector2 targetPos, float duration = 0.3f)
    {
        view.KillActiveAnimation();
        return view.transform.DOMove(targetPos, duration).SetEase(Ease.InBack).SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="view"></param>
    /// <param name="targetPos"></param>
    /// <param name="tileSize"></param>
    /// <param name="duration">Root movement time</param>
    /// <param name="moveEase"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static UniTask PlayJellyishMove(PetalView view, Vector2 targetPos, float tileSize, float duration, Ease moveEase)
    {
        if (tileSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(tileSize),
                "Directional jelly movement requires a positive tile size.");
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
            return view.transform.DOMove(targetPos, duration).SetEase(moveEase).SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());

        PrepareDirectionalJelly(view, displacement, tileSize, duration, out Vector3 stretchScale,
            out Vector3 squashScale);

        float squashDuration = duration * view.DirectionalJellySquashDurationRatio;
        float settleDuration = duration * view.DirectionalJellySettleDurationRatio;

        Sequence seq = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        seq.SetTarget(view.transform);
        seq.Join(view.transform.DOMove(targetPos, duration).SetEase(moveEase));
        seq.Join(view.StretchAxis.DOScale(stretchScale, duration).SetEase(Ease.OutQuad));
        seq.Append(view.StretchAxis.DOScale(squashScale, squashDuration).SetEase(Ease.InOutSine));
        seq.Append(view.StretchAxis.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutBack));
        seq.OnKill(() =>
        {
            if (view != null)
                view.ResetDirectionalJelly();
        });
        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    private static void PrepareDirectionalJelly(PetalView view, Vector2 displacement, float tileSize, float duration, out Vector3 stretchScale, out Vector3 squashScale)
    {
        float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;
        float speedTilesPerSecond = displacement.magnitude / tileSize / duration;
        float stretchAmount = Mathf.Clamp(speedTilesPerSecond * view.DirectionalJellyStrength, 0f, view.MaxDirectionalJellyStretch);

        view.StretchAxis.localRotation = Quaternion.Euler(0f, 0f, angle);
        view.VisualTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

        stretchScale = new Vector3(1f + stretchAmount, 1f - stretchAmount, 1f);
        squashScale = new Vector3(1f - stretchAmount, 1f + stretchAmount, 1f);
    }   

    public static async UniTask PlayFly(PetalView view, Vector2 targetWorldPosition, float tileSize, float duration, float curveAmplitudeInTiles)
    {
        view.KillActiveAnimation();
        Vector3 startControlPoint = view.transform.position + Vector3.up * tileSize * curveAmplitudeInTiles;
        Vector3 endControlPoint = (Vector3)targetWorldPosition + Vector3.up * tileSize * curveAmplitudeInTiles;
        Vector3[] flightPath = { targetWorldPosition, startControlPoint, endControlPoint };
        Vector2 previousPosition = view.transform.position;

        Tween scaleTween = view.transform.DOScale(view.DefaultRootScale, duration).SetEase(Ease.InQuad)
            .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        Tween flapTween = view.VisualTransform.DOScaleX(0f, 0.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);

        try
        {
            await view.transform.DOPath(flightPath, duration, PathType.CubicBezier, PathMode.Sidescroller2D)
                .OnUpdate(() =>
                {
                    Vector2 currentPosition = view.transform.position;
                    Vector2 flightDirection = currentPosition - previousPosition;
                    if (flightDirection.sqrMagnitude > 0f)
                    {
                        float angle = Vector2.SignedAngle(Vector2.up, flightDirection);
                        view.VisualTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    }
                    previousPosition = currentPosition;
                })
                .SetEase(Ease.InQuad)
                .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
        }
        finally
        {
            scaleTween.Kill();
            flapTween.Kill();
            if (view != null)
            {
                view.transform.localScale = view.DefaultRootScale;
                view.VisualTransform.localScale = view.TargetScale;
            }
        }
    }

    public static async UniTask PlayPetalChange(PetalView view, Petal petal, float tileSize, float duration)
    {
        view.KillVisualAnimation();
        await view.VisualTransform.DOScale(Vector3.zero, duration * 0.4f)
            .SetEase(Ease.InBack)
            .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());

        view.Init(petal, tileSize);
        view.VisualTransform.localScale = Vector3.zero;

        await view.VisualTransform.DOScale(view.TargetScale, duration * 0.6f)
            .SetEase(Ease.OutBack)
            .SetLink(view.gameObject, LinkBehaviour.KillOnDestroy)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    public static UniTask PlayComboMerge(PetalView viewA, PetalView viewB)
    {
        viewA.KillActiveAnimation();
        viewB.KillActiveAnimation();
        Vector3 midpoint = (viewA.transform.position + viewB.transform.position) / 2f;

        Sequence seq = DOTween.Sequence().SetLink(viewA.gameObject, LinkBehaviour.KillOnDestroy);

        // Stand up - quick anticipation bounce
        seq.Append(viewA.VisualTransform.DOScale(viewA.TargetScale * 1.2f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(viewB.VisualTransform.DOScale(viewB.TargetScale * 1.2f, 0.15f).SetEase(Ease.OutQuad));

        // Fly to each other
        seq.Append(viewA.transform.DOMove(midpoint, 0.25f).SetEase(Ease.InOutQuad));
        seq.Join(viewB.transform.DOMove(midpoint, 0.25f).SetEase(Ease.InOutQuad));

        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, viewA.GetCancellationTokenOnDestroy());
    }

    public static UniTask PlayComboSpinAndDisappear(PetalView viewA, PetalView viewB, float duration)
    {
        viewA.KillVisualAnimation();
        viewB.KillVisualAnimation();
        Sequence seq = DOTween.Sequence().SetLink(viewA.gameObject, LinkBehaviour.KillOnDestroy);
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

        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, viewA.GetCancellationTokenOnDestroy());
    }
}
