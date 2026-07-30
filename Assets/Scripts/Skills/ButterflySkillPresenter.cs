using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public sealed class ButterflySkillPresenter : SkillRepresentationPresenter<ButterflyRepresentationData>
    {
        private const float FlightCurveAmplitudeInTiles = 2f;
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
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardLayout layout;
        private readonly float prepareDuration;
        private readonly float fireDuration;
        private readonly float finishDuration;

        public ButterflySkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardLayout layout, float prepareDuration, float fireDuration, float finishDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
            this.prepareDuration = prepareDuration;
            this.fireDuration = fireDuration;
            this.finishDuration = finishDuration;
        }

        protected override async UniTask Play(ButterflyRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var changes = new List<TileChange>();
            foreach (TileChange change in resolution.TileChanges)
            {
                if (change.ObstacleLayerChanged)
                    changes.Add(change);
            }
            if (!representation.Target.HasValue)
            {
                IReadOnlyList<Vector2Int> source = accessKeys.ContainsKey(representation.Source) ? new[] { representation.Source } : System.Array.Empty<Vector2Int>();
                await UniTask.WhenAll(petalViewManager.PlayDisappearAndRelease(source, finishDuration, accessKeys), tileViewManager.PlayTileChanges(changes));
                return;
            }

            VFXButterflySkill butterflyVFX = await Prepare(representation.Source, representation.SourcePetalType, accessKeys);
            await Fire(butterflyVFX, representation.Source, representation.Target.Value, accessKeys);
            await Finish(butterflyVFX, representation.Source, representation.Target.Value, resolution, changes, accessKeys);
        }

        private async UniTask<VFXButterflySkill> Prepare(Vector2Int source, PetalType sourcePetalType, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            if (!accessKeys.ContainsKey(source))
                return null;

            Transform visualTransform = petalViewManager.GetAccessibleVisualTransform(source, accessKeys);
            VFXButterflySkill butterflyVFX = boardVFXManager.RentButterflySkillVFX(visualTransform);
            butterflyVFX.SetColor(ParticleColors[sourcePetalType]);
            await UniTask.WhenAll(butterflyVFX.Prepare(prepareDuration), petalViewManager.PlayRootScale(source, 1.5f, prepareDuration, accessKeys));
            return butterflyVFX;
        }

        private async UniTask Fire(VFXButterflySkill butterflyVFX, Vector2Int source, Vector2Int target, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            if (butterflyVFX == null)
                return;

            butterflyVFX.Fire();
            await petalViewManager.PlayFly(source, target, layout, fireDuration, FlightCurveAmplitudeInTiles, accessKeys);
        }

        private async UniTask Finish(VFXButterflySkill butterflyVFX, Vector2Int source, Vector2Int target, MatchGroupResolveResult resolution, IReadOnlyList<TileChange> changes, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            bool targetNeedsView = target != source && (SkillPresentationQueries.WasPetalRemovedAt(resolution, target) || resolution.IsTriggeredSkillInputPosition(target));
            if (targetNeedsView && !accessKeys.ContainsKey(target) && petalViewManager.TryAcquireView(target, nameof(ButterflySkillPresenter), out ViewAccessKey targetAccessKey))
                accessKeys.Add(target, targetAccessKey);
            var disappearingPositions = new List<Vector2Int>();
            if (accessKeys.ContainsKey(source))
                disappearingPositions.Add(source);
            var triggeredSkillPositions = new List<Vector2Int>();
            if (SkillPresentationQueries.WasPetalRemovedAt(resolution, target) && target != source && !resolution.IsTriggeredSkillInputPosition(target) && accessKeys.ContainsKey(target))
                disappearingPositions.Add(target);
            if (resolution.IsTriggeredSkillInputPosition(target) && target != source && accessKeys.ContainsKey(target))
                triggeredSkillPositions.Add(target);

            if (butterflyVFX != null)
            {
                boardVFXManager.FinishButterflySkillVFX(butterflyVFX, finishDuration).Forget();
            }

            await UniTask.WhenAll(petalViewManager.PlayDisappearAndRelease(disappearingPositions, finishDuration, accessKeys), petalViewManager.PlayAboutToExecute(triggeredSkillPositions, accessKeys), tileViewManager.PlayTileChanges(changes));
        }
    }
}
