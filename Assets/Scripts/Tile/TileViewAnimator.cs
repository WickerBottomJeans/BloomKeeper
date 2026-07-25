using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DG.Tweening;
using UnityEngine;

public static class TileViewAnimator
{
    public static async UniTask PlayOverlayTransition(TileView view, Sprite incoming)
    {
        view.PrepareOverlayAnimation(view.OverlayRenderer.sprite);

        view.SetOverlay(incoming);
        view.OverlayRenderer.color = new Color(1f, 1f, 1f, 0f);
        view.OverlayRenderer.transform.localScale = Vector3.zero;

        Vector3 outgoingTargetScale = view.GetTargetScale(view.OverlayAnimationRenderer);
        Vector3 incomingTargetScale = view.GetTargetScale(view.OverlayRenderer);

        Sequence outgoing = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        Tween outgoingScale = view.OverlayAnimationRenderer.transform.DOScale(outgoingTargetScale * 1.3f, 0.15f).SetEase(Ease.OutQuad);
        Tween outgoingFade = view.OverlayAnimationRenderer.DOFade(0f, 0.15f);
        _ = outgoing.Append(outgoingScale);
        _ = outgoing.Join(outgoingFade);

        Sequence incomingSequence = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        Tween incomingScale = view.OverlayRenderer.transform.DOScale(incomingTargetScale, 0.2f).SetEase(Ease.OutBack);
        Tween incomingFade = view.OverlayRenderer.DOFade(1f, 0.2f);
        _ = incomingSequence.Append(incomingScale);
        _ = incomingSequence.Join(incomingFade);

        await UniTask.WhenAll(
            outgoing.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy()),
            incomingSequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy()));

        view.ClearOverlayAnimation();
    }

    public static UniTask PlayOverlaySpawn(TileView view)
    {
        Vector3 targetScale = view.GetTargetScale(view.OverlayRenderer);
        view.OverlayRenderer.color = new Color(1f, 1f, 1f, 0f);
        view.OverlayRenderer.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(view.OverlayRenderer.transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutBack));
        seq.Join(view.OverlayRenderer.DOFade(1f, 0.2f));
        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }

    public static UniTask PlayOverlayDespawn(TileView view)
    {
        Vector3 targetScale = view.GetTargetScale(view.OverlayRenderer);

        Sequence seq = DOTween.Sequence().SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(view.OverlayRenderer.transform.DOScale(targetScale * 1.3f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(view.OverlayRenderer.DOFade(0f, 0.15f));
        return seq.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, view.GetCancellationTokenOnDestroy());
    }
}
