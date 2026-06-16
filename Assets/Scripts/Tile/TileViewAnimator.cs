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

        Sequence outgoing = DOTween.Sequence();
        outgoing.Append(view.OverlayAnimationRenderer.transform
            .DOScale(outgoingTargetScale * 1.3f, 0.15f).SetEase(Ease.OutQuad));
        outgoing.Join(view.OverlayAnimationRenderer.DOFade(0f, 0.15f));

        Sequence incoming_seq = DOTween.Sequence();
        incoming_seq.Append(view.OverlayRenderer.transform
            .DOScale(incomingTargetScale, 0.2f).SetEase(Ease.OutBack));
        incoming_seq.Join(view.OverlayRenderer.DOFade(1f, 0.2f));

        await UniTask.WhenAll(outgoing.ToUniTask(), incoming_seq.ToUniTask());

        view.ClearOverlayAnimation();
    }

    public static UniTask PlayOverlaySpawn(TileView view)
    {
        Vector3 targetScale = view.GetTargetScale(view.OverlayRenderer);
        view.OverlayRenderer.color = new Color(1f, 1f, 1f, 0f);
        view.OverlayRenderer.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(view.OverlayRenderer.transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutBack));
        seq.Join(view.OverlayRenderer.DOFade(1f, 0.2f));
        return seq.ToUniTask();
    }

    public static UniTask PlayOverlayDespawn(TileView view)
    {
        Vector3 targetScale = view.GetTargetScale(view.OverlayRenderer);

        Sequence seq = DOTween.Sequence();
        seq.Append(view.OverlayRenderer.transform.DOScale(targetScale * 1.3f, 0.15f).SetEase(Ease.OutQuad));
        seq.Join(view.OverlayRenderer.DOFade(0f, 0.15f));
        return seq.ToUniTask();
    }
}