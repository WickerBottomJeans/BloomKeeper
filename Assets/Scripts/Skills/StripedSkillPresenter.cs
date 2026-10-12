using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using DG.Tweening;
using UnityEngine;

namespace Skills
{
    public class StripedSkillPresenter : SkillAnimation<StripedRepresentationData>
    {
        private const float SourcePetalPrepareScale = 1.5f;
        private const float PrepareDuration = 0.15f;
        private const float FireDuration = 0.2f;
        private const float FinishDuration = 0.1f;

        private readonly PieceViewManager pieceViewManager;
        private readonly BoardVFXManager boardVFXManager;

        public StripedSkillPresenter(PieceViewManager pieceViewManager, BoardVFXManager boardVFXManager)
        {
            this.pieceViewManager = pieceViewManager;
            this.boardVFXManager = boardVFXManager;
        }

        protected override async UniTask Play(StripedRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            bool isVertical = representation.Direction == SpecialSkillType.StripedVertical;
            VFXStripeBeamAxis beamAxis = boardVFXManager.RentStripedBeamAxisVFX(representation.Source);
            VFXStripeHalo halo = boardVFXManager.RentStripedHaloVFX(representation.Source);

            try
            {
                await Prepare(halo, representation.Source, skillPresentationContext.Views, skillPresentationContext.AudioPlaybackScope);
                await Fire(beamAxis, representation.Source, isVertical);
                await Finish(beamAxis, halo, representation.Source, skillPresentationContext);
            }
            finally
            {
                boardVFXManager.ReleaseStripedBeamAxisVFX(beamAxis);
                boardVFXManager.ReleaseStripedHaloVFX(halo);
            }
        }

        private UniTask Prepare(VFXStripeHalo halo, Vector2Int source, PieceViewScope views, AudioPlaybackScope audioScope)
        {
            return UniTask.WhenAll(halo.Prepare(PrepareDuration, audioScope),
                pieceViewManager.PlayScale(source, SourcePetalPrepareScale, PrepareDuration, Ease.OutCubic, views));
        }

        private UniTask Fire(VFXStripeBeamAxis beamAxis, Vector2Int source, bool isVertical)
        {
            return boardVFXManager.FireStripedBeamAxisVFX(beamAxis, source, isVertical, FireDuration);
        }

        private async UniTask Finish(VFXStripeBeamAxis beamAxis, VFXStripeHalo halo, Vector2Int source, SkillPresentationContext skillPresentationContext)
        {
            await skillPresentationContext.ClearImmediately(pieceViewManager, source);
            await UniTask.WhenAll(beamAxis.Finish(FinishDuration), halo.Finish(FinishDuration));
        }
    }
}
