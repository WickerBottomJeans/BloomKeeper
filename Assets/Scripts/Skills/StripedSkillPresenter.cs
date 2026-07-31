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
    public sealed class StripedSkillPresenter : SkillRepresentationPresenter<StripedRepresentationData>
    {
        private const float SourcePetalPrepareScale = 1.5f;

        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly float prepareDuration;
        private readonly float fireDuration;
        private readonly float finishDuration;

        public StripedSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, float prepareDuration, float fireDuration, float finishDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.prepareDuration = prepareDuration;
            this.fireDuration = fireDuration;
            this.finishDuration = finishDuration;
        }

        protected override async UniTask Play(StripedRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            bool isVertical = representation.Direction == SpecialSkillType.StripedVertical;
            VFXStripeBeamAxis beamAxis = boardVFXManager.RentStripedBeamAxisVFX(representation.Source);
            VFXStripeHalo halo = boardVFXManager.RentStripedHaloVFX(representation.Source);

            try
            {
                await Prepare(halo, representation.Source, accessKeys, audioScope);
                await Fire(beamAxis, representation.Source, isVertical);
                await Finish(beamAxis, halo, representation.Source, resolution, accessKeys);
            }
            finally
            {
                boardVFXManager.ReleaseStripedBeamAxisVFX(beamAxis);
                boardVFXManager.ReleaseStripedHaloVFX(halo);
            }
        }

        private UniTask Prepare(VFXStripeHalo halo, Vector2Int source, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            return UniTask.WhenAll(halo.Prepare(prepareDuration, audioScope),
                petalViewManager.PlayScale(source, SourcePetalPrepareScale, prepareDuration, Ease.OutCubic, accessKeys));
        }

        private UniTask Fire(VFXStripeBeamAxis beamAxis, Vector2Int source, bool isVertical)
        {
            return boardVFXManager.FireStripedBeamAxisVFX(beamAxis, source, isVertical, fireDuration);
        }

        private async UniTask Finish(VFXStripeBeamAxis beamAxis, VFXStripeHalo halo, Vector2Int source, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            HashSet<Vector2Int> removedPositions = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(resolution);
            removedPositions.Add(source);
            var ownedRemovedPositions = new List<Vector2Int>();
            foreach (Vector2Int position in removedPositions)
            {
                if (!accessKeys.ContainsKey(position) && petalViewManager.TryAcquireView(position, nameof(StripedSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
                if (accessKeys.ContainsKey(position))
                    ownedRemovedPositions.Add(position);
            }

            var triggeredSkillPositions = new List<Vector2Int>();
            foreach (Vector2Int position in resolution.GetTriggeredSkillInputPositions())
            {
                if (!accessKeys.ContainsKey(position) && petalViewManager.TryAcquireView(position, nameof(StripedSkillPresenter), out ViewAccessKey accessKey))
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
            await UniTask.WhenAll(beamAxis.Finish(finishDuration), halo.Finish(finishDuration), petalViewManager.PlayAboutToExecute(triggeredSkillPositions, accessKeys), tileViewManager.PlayTileChanges(obstacleChanges));
        }
    }
}
