using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public sealed class StripeStripeFusionPresenter : SkillRepresentationPresenter<StripeStripeFusionRepresentationData>
    {
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly float prepareDuration;
        private readonly float fireDuration;
        private readonly float finishDuration;

        public StripeStripeFusionPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, float prepareDuration, float fireDuration, float finishDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.prepareDuration = prepareDuration;
            this.fireDuration = fireDuration;
            this.finishDuration = finishDuration;
        }

        protected override async UniTask Play(StripeStripeFusionRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            VFXStripeHalo halo = boardVFXManager.RentStripedHaloVFX(representation.Anchor);
            VFXStripeBeamAxis horizontalBeam = boardVFXManager.RentStripedBeamAxisVFX(representation.Anchor);
            VFXStripeBeamAxis verticalBeam = boardVFXManager.RentStripedBeamAxisVFX(representation.Anchor);

            try
            {
                await Prepare(halo, representation.ConsumedInputPositions, accessKeys);
                await Fire(horizontalBeam, verticalBeam, representation.Anchor);
                await Finish(horizontalBeam, verticalBeam, halo, representation, resolution, accessKeys);
            }
            finally
            {
                boardVFXManager.ReleaseStripedBeamAxisVFX(horizontalBeam);
                boardVFXManager.ReleaseStripedBeamAxisVFX(verticalBeam);
                boardVFXManager.ReleaseStripedHaloVFX(halo);
            }
        }

        private UniTask Prepare(VFXStripeHalo halo, IReadOnlyList<Vector2Int> consumedInputPositions, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            return UniTask.WhenAll(halo.Prepare(prepareDuration), petalViewManager.PlayNormalRemovals(consumedInputPositions, accessKeys));
        }

        private UniTask Fire(VFXStripeBeamAxis horizontalBeam, VFXStripeBeamAxis verticalBeam, Vector2Int anchor)
        {
            return UniTask.WhenAll(
                boardVFXManager.FireStripedBeamAxisVFX(horizontalBeam, anchor, false, fireDuration),
                boardVFXManager.FireStripedBeamAxisVFX(verticalBeam, anchor, true, fireDuration));
        }

        private async UniTask Finish(VFXStripeBeamAxis horizontalBeam, VFXStripeBeamAxis verticalBeam, VFXStripeHalo halo, StripeStripeFusionRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            HashSet<Vector2Int> removedPositions = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(resolution);
            var ownedRemovedPositions = new List<Vector2Int>();
            foreach (Vector2Int position in removedPositions)
            {
                if (!accessKeys.ContainsKey(position) && petalViewManager.TryAcquireView(position, nameof(StripeStripeFusionPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
                if (accessKeys.ContainsKey(position))
                    ownedRemovedPositions.Add(position);
            }

            var triggeredSkillPositions = new List<Vector2Int>();
            foreach (Vector2Int position in resolution.GetTriggeredSkillInputPositions())
            {
                if (!accessKeys.ContainsKey(position) && petalViewManager.TryAcquireView(position, nameof(StripeStripeFusionPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
                if (accessKeys.ContainsKey(position))
                    triggeredSkillPositions.Add(position);
            }

            var obstacleChanges = new List<TileChange>();
            foreach (TileChange change in resolution.TileChanges)
            {
                if (change.ObstacleLayerChanged)
                    obstacleChanges.Add(change);
            }

            petalViewManager.ReleasePetalViewsImmediately(ownedRemovedPositions, accessKeys);
            await UniTask.WhenAll(horizontalBeam.Finish(finishDuration), verticalBeam.Finish(finishDuration), halo.Finish(finishDuration), petalViewManager.PlayAboutToExecute(triggeredSkillPositions, accessKeys), tileViewManager.PlayTileChanges(obstacleChanges));
        }
    }
}
