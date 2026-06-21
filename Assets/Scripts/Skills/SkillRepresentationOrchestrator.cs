using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public class SkillRepresentationOrchestrator : MonoBehaviour
    {
        [Header("Bouquet Timing")]
        [SerializeField] private float bouquetDisappearDuration = 0.2f;

        [Header("Striped Timing")]
        [SerializeField] private float stripedPropagationDuration = 0.2f;

        [Header("Butterfly Timing")]
        [SerializeField] private float butterflyFlightDuration = 0.5f;
        [SerializeField] private float butterflyDisappearDuration = 0.2f;

        [Header("Stripe Sunburst Timing")]
        [SerializeField] private float stripeSunburstSpinDuration = 2f;
        [SerializeField] private float stripeSunburstMutationDuration = 1f;
        [SerializeField] private float stripeSunburstLaserDuration = 1f;
        [SerializeField] private float stripeSunburstLaserChargeUpDuration = 0.5f;

        private PetalViewManager petalViewManager;
        private BoardVFXManager boardVFXManager;
        private BoardLayout layout;

        public void Init(
            PetalViewManager petalViewManager,
            BoardVFXManager boardVFXManager,
            BoardLayout layout)
        {
            this.petalViewManager = petalViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
        }

        public async UniTask Play(IReadOnlyList<SkillUseResult> skillResults)
        {
            var tasks = new List<UniTask>(skillResults.Count);

            foreach (SkillUseResult result in skillResults)
                tasks.Add(Play(result.Representation));

            await UniTask.WhenAll(tasks);
        }

        private async UniTask Play(SkillRepresentationData representation)
        {
            switch (representation)
            {
                case null:
                    return;
                case BouquetRepresentationData bouquet:
                    await PlayBouquet(bouquet);
                    return;
                case StripedRepresentationData striped:
                    bool isVertical = striped.Direction == SpecialSkillType.StripedVertical;
                    await UniTask.WhenAll(
                        boardVFXManager.PlayStripedSkillVFX(striped.Source, isVertical, stripedPropagationDuration),
                        petalViewManager.PlayStripedDisappear(striped.Source, isVertical, stripedPropagationDuration));
                    return;
                case ButterflyRepresentationData butterfly:
                    await PlayButterfly(butterfly);
                    return;
                case StripeSunburstRepresentationData stripeSunburst:
                    await PlayStripeSunburst(stripeSunburst);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(representation),
                        representation.GetType(),
                        "Skill representation is not supported.");
            }
        }

        private async UniTask PlayBouquet(BouquetRepresentationData representation)
        {
            UniTask disappearTask = petalViewManager.PlayDisappearAndRelease(
                representation.AffectedPositions,
                bouquetDisappearDuration);

            UniTask bloomTask = boardVFXManager.PlayBouquetBloomVFX(representation.Center);

            await UniTask.WhenAll(disappearTask, bloomTask);
        }

        private async UniTask PlayButterfly(ButterflyRepresentationData representation)
        {
            if (!representation.Target.HasValue)
            {
                await petalViewManager.PlayDisappearAndRelease(new[] { representation.Source }, butterflyDisappearDuration);
                return;
            }

            await petalViewManager.PlayFly(representation.Source, representation.Target.Value, layout, butterflyFlightDuration);
            await petalViewManager.PlayDisappearAndRelease(new[] { representation.Source, representation.Target.Value }, butterflyDisappearDuration);
        }

        private async UniTask PlayStripeSunburst(StripeSunburstRepresentationData representation)
        {
            UniTask mergeTask = petalViewManager.PlayComboMerge(representation.SourceA, representation.SourceB);

            UniTask spinTask = petalViewManager.PlayComboSpinAndRelease(
                representation.SourceA,
                representation.SourceB,
                stripeSunburstSpinDuration);
            
            UniTask laserTask = boardVFXManager.PlayMutationLaserVFX(
                representation.Origin,
                representation.Changes,
                stripeSunburstLaserChargeUpDuration,
                stripeSunburstLaserDuration);

            await UniTask.Delay(TimeSpan.FromSeconds(stripeSunburstLaserChargeUpDuration));
            
            UniTask mutationTask = petalViewManager.OnPetalsChanged(
                representation.Changes,
                layout,
                stripeSunburstMutationDuration);

            await UniTask.WhenAll(
                mergeTask,
                spinTask,
                mutationTask,
                laserTask);
        }
    }
}
