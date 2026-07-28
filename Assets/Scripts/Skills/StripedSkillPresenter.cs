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
        private const float PrepareDuration = 0.15f;
        private const float FireDuration = 0.2f;
        private const float FinishDuration = 0.1f;
        private const float SourcePetalPrepareScale = 1.5f;

        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardAudioManager boardAudioManager;

        public StripedSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardAudioManager boardAudioManager)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.boardAudioManager = boardAudioManager;
        }

        protected override void AcquireVitalViews(StripedRepresentationData representation,
            MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            if (accessKeys.ContainsKey(representation.Source)) return;
            if (petalViewManager.TryAcquireView(representation.Source, nameof(StripedSkillPresenter),
                    out ViewAccessKey accessKey))
                accessKeys.Add(representation.Source, accessKey);
        }

        protected override async UniTask Play(StripedRepresentationData representation,
            MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            bool isVertical = representation.Direction == SpecialSkillType.StripedVertical;
            VFXStripeSkill stripe = boardVFXManager.RentStripedSkillVFX(representation.Source);

            try
            {
                boardAudioManager.PlayStripedSkill();
                await Prepare(stripe, representation.Source, accessKeys);
                await Fire(stripe, representation.Source, isVertical);
                await Finish(stripe, representation.Source, resolution, accessKeys);
            }
            finally
            {
                boardVFXManager.ReleaseStripedSkillVFX(stripe);
            }
        }

        private UniTask Prepare(VFXStripeSkill stripe, Vector2Int source,
            IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            return UniTask.WhenAll(stripe.Prepare(PrepareDuration),
                petalViewManager.PlayScale(source, SourcePetalPrepareScale, PrepareDuration, Ease.OutCubic, accessKeys));
        }

        private UniTask Fire(VFXStripeSkill stripe, Vector2Int source, bool isVertical)
        {
            return boardVFXManager.FireStripedSkillVFX(stripe, source, isVertical, FireDuration);
        }

        private async UniTask Finish(VFXStripeSkill stripe, Vector2Int source, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
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
            foreach (Vector2Int position in resolution.GetSkillTriggerPositions())
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
            await UniTask.WhenAll(stripe.Finish(FinishDuration), petalViewManager.PlayAboutToExecuteShake(triggeredSkillPositions, accessKeys), tileViewManager.PlayTileChanges(obstacleChanges));
        }
    }
}
