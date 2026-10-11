using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public class ButterflySkillPresenter : SkillAnimation<ButterflyRepresentationData>
    {
        private const float FlightCurveAmplitudeInTiles = 2f;
        private const float SourcePetalPrepareScale = 1.5f;
        private const float PrepareDuration = 0.1f;
        private const float FireDuration = 0.7f;
        private const float FinishDuration = 0.1f;
        private static readonly IReadOnlyDictionary<PetalType, Color> ParticleColors = new Dictionary<PetalType, Color>
        {
            { PetalType.Strawberry, new Color32(255, 105, 120, 255) },
            { PetalType.Mushroom, new Color32(255, 155, 65, 255) },
            { PetalType.Starfruit, new Color32(255, 225, 75, 255) },
            { PetalType.Clover, new Color32(175, 255, 105, 255) },
            { PetalType.Dewdrop, new Color32(85, 215, 255, 255) },
            { PetalType.BerryCluster, new Color32(205, 115, 255, 255) },
            { PetalType.Daisy, new Color32(255, 245, 205, 255) }
        };

        private readonly PetalViewManager petalViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardLayout layout;

        public ButterflySkillPresenter(PetalViewManager petalViewManager, BoardVFXManager boardVFXManager, BoardLayout layout)
        {
            this.petalViewManager = petalViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
        }

        protected override async UniTask Play(ButterflyRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            PetalViewScope views = skillPresentationContext.Views;
            if (!representation.Target.HasValue)
            {
                IReadOnlyList<Vector2Int> source = views.Owns(representation.Source) ? new[] { representation.Source } : System.Array.Empty<Vector2Int>();
                await petalViewManager.PlayDisappearAndRelease(source, FinishDuration, views);
                return;
            }

            VFXButterflySkill butterflyVFX = await Prepare(representation.Source, representation.SourcePetalType, views, skillPresentationContext.AudioPlaybackScope);
            await Fire(butterflyVFX, representation.Source, representation.Target.Value, views);
            await Finish(butterflyVFX, representation.Source, representation.Target.Value, skillPresentationContext);
        }

        private async UniTask<VFXButterflySkill> Prepare(Vector2Int source, PetalType sourcePetalType, PetalViewScope views, AudioPlaybackScope audioScope)
        {
            if (!views.Owns(source))
                return null;

            Transform visualTransform = petalViewManager.GetAccessibleVisualTransform(source, views);
            VFXButterflySkill butterflyVFX = boardVFXManager.RentButterflySkillVFX(visualTransform);
            butterflyVFX.SetColor(ParticleColors[sourcePetalType]);
            await UniTask.WhenAll(butterflyVFX.Prepare(PrepareDuration, audioScope), petalViewManager.PlayRootScale(source, SourcePetalPrepareScale, PrepareDuration, views));
            return butterflyVFX;
        }

        private async UniTask Fire(VFXButterflySkill butterflyVFX, Vector2Int source, Vector2Int target, PetalViewScope views)
        {
            if (butterflyVFX == null)
                return;

            butterflyVFX.Fire();
            await petalViewManager.PlayFly(source, target, layout, FireDuration, FlightCurveAmplitudeInTiles, views);
        }

        private async UniTask Finish(VFXButterflySkill butterflyVFX, Vector2Int source, Vector2Int target, SkillPresentationContext skillPresentationContext)
        {
            BoardChangeGroup resolution = skillPresentationContext.BoardChangeGroup;
            PetalViewScope views = skillPresentationContext.Views;
            bool targetNeedsView = target != source && (SkillPresentationQueries.WasPetalRemovedAt(resolution, target) || resolution.IsTriggeredSkillInputPosition(target));
            if (targetNeedsView) views.TryAcquire(target);
            var disappearingPositions = new List<Vector2Int>();
            if (views.Owns(source))
                disappearingPositions.Add(source);
            var triggeredSkillPositions = new List<Vector2Int>();
            if (SkillPresentationQueries.WasPetalRemovedAt(resolution, target) && target != source && !resolution.IsTriggeredSkillInputPosition(target) && views.Owns(target))
                disappearingPositions.Add(target);
            if (resolution.IsTriggeredSkillInputPosition(target) && target != source && views.Owns(target))
                triggeredSkillPositions.Add(target);

            if (butterflyVFX != null)
            {
                boardVFXManager.FinishButterflySkillVFX(butterflyVFX, FinishDuration, skillPresentationContext.AudioPlaybackScope).Forget();
            }

            UniTask disappearanceTask = petalViewManager.PlayDisappearAndRelease(disappearingPositions, FinishDuration, views);
            UniTask triggerTask = petalViewManager.PlayAboutToExecute(triggeredSkillPositions, views);
            skillPresentationContext.HandleTileImpactPresented(target);
            await UniTask.WhenAll(disappearanceTask, triggerTask);
        }
    }
}
