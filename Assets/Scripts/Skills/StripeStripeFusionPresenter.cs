using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public class StripeStripeFusionPresenter : SkillRepresentationPresenter<StripeStripeFusionRepresentationData>
    {
        private const float PrepareDuration = 0.15f;
        private const float FireDuration = 0.2f;
        private const float FinishDuration = 0.1f;

        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;

        public StripeStripeFusionPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
        }

        protected override async UniTask Play(StripeStripeFusionRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            VFXStripeHalo halo = boardVFXManager.RentStripedHaloVFX(representation.Anchor);
            VFXStripeBeamAxis horizontalBeam = boardVFXManager.RentStripedBeamAxisVFX(representation.Anchor);
            VFXStripeBeamAxis verticalBeam = boardVFXManager.RentStripedBeamAxisVFX(representation.Anchor);

            try
            {
                await Prepare(halo, representation.ConsumedInputPositions, accessKeys, audioScope);
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

        private UniTask Prepare(VFXStripeHalo halo, IReadOnlyList<Vector2Int> consumedInputPositions, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            return UniTask.WhenAll(halo.Prepare(PrepareDuration, audioScope), petalViewManager.PlayNormalRemovals(consumedInputPositions, accessKeys));
        }

        private UniTask Fire(VFXStripeBeamAxis horizontalBeam, VFXStripeBeamAxis verticalBeam, Vector2Int anchor)
        {
            return UniTask.WhenAll(
                boardVFXManager.FireStripedBeamAxisVFX(horizontalBeam, anchor, false, FireDuration),
                boardVFXManager.FireStripedBeamAxisVFX(verticalBeam, anchor, true, FireDuration));
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
            await UniTask.WhenAll(horizontalBeam.Finish(FinishDuration), verticalBeam.Finish(FinishDuration), halo.Finish(FinishDuration), petalViewManager.PlayAboutToExecute(triggeredSkillPositions, accessKeys), tileViewManager.PlayTileChanges(obstacleChanges));
        }
    }
}
