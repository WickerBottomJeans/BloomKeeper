using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public class StripeStripeFusionPresenter : SkillAnimation<StripeStripeFusionRepresentationData>
    {
        private const float PrepareDuration = 0.15f;
        private const float FireDuration = 0.2f;
        private const float FinishDuration = 0.1f;

        private readonly PetalViewManager petalViewManager;
        private readonly BoardVFXManager boardVFXManager;

        public StripeStripeFusionPresenter(PetalViewManager petalViewManager, BoardVFXManager boardVFXManager)
        {
            this.petalViewManager = petalViewManager;
            this.boardVFXManager = boardVFXManager;
        }

        protected override async UniTask Play(StripeStripeFusionRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            VFXStripeHalo halo = boardVFXManager.RentStripedHaloVFX(representation.Anchor);
            VFXStripeBeamAxis horizontalBeam = boardVFXManager.RentStripedBeamAxisVFX(representation.Anchor);
            VFXStripeBeamAxis verticalBeam = boardVFXManager.RentStripedBeamAxisVFX(representation.Anchor);

            try
            {
                await Prepare(halo, representation.ConsumedInputPositions, skillPresentationContext);
                await Fire(horizontalBeam, verticalBeam, representation.Anchor);
                await Finish(horizontalBeam, verticalBeam, halo, representation, skillPresentationContext);
            }
            finally
            {
                boardVFXManager.ReleaseStripedBeamAxisVFX(horizontalBeam);
                boardVFXManager.ReleaseStripedBeamAxisVFX(verticalBeam);
                boardVFXManager.ReleaseStripedHaloVFX(halo);
            }
        }

        private UniTask Prepare(VFXStripeHalo halo, IReadOnlyList<Vector2Int> consumedInputPositions, SkillPresentationContext skillPresentationContext)
        {
            UniTask haloTask = halo.Prepare(PrepareDuration, skillPresentationContext.AudioPlaybackScope);
            UniTask removalTask = petalViewManager.PlayNormalRemovals(consumedInputPositions, skillPresentationContext.Views);
            foreach (Vector2Int position in consumedInputPositions)
                skillPresentationContext.HandleConsumedInputPresented(position);
            return UniTask.WhenAll(haloTask, removalTask);
        }

        private UniTask Fire(VFXStripeBeamAxis horizontalBeam, VFXStripeBeamAxis verticalBeam, Vector2Int anchor)
        {
            return UniTask.WhenAll(
                boardVFXManager.FireStripedBeamAxisVFX(horizontalBeam, anchor, false, FireDuration),
                boardVFXManager.FireStripedBeamAxisVFX(verticalBeam, anchor, true, FireDuration));
        }

        private async UniTask Finish(VFXStripeBeamAxis horizontalBeam, VFXStripeBeamAxis verticalBeam, VFXStripeHalo halo, StripeStripeFusionRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            await skillPresentationContext.ClearImmediately(petalViewManager);
            await UniTask.WhenAll(horizontalBeam.Finish(FinishDuration), verticalBeam.Finish(FinishDuration), halo.Finish(FinishDuration));
        }
    }
}
