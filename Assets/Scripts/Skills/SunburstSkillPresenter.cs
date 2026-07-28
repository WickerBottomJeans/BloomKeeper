using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public sealed class SunburstSkillPresenter : SkillRepresentationPresenter<SunburstRepresentationData>
    {
        private readonly PetalViewManager petalViewManager;
        private readonly TileViewManager tileViewManager;
        private readonly BoardVFXManager boardVFXManager;
        private readonly BoardLayout layout;
        private readonly float spinDuration;
        private readonly float mutationDuration;
        private readonly float laserDuration;
        private readonly float laserChargeUpDuration;

        public SunburstSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardLayout layout, float spinDuration, float mutationDuration, float laserDuration, float laserChargeUpDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
            this.spinDuration = spinDuration;
            this.mutationDuration = mutationDuration;
            this.laserDuration = laserDuration;
            this.laserChargeUpDuration = laserChargeUpDuration;
        }

        protected override void AcquireVitalViews(SunburstRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            var positions = new HashSet<Vector2Int>();
            positions.Add(representation.ParticipantA);
            if (representation.ParticipantB.HasValue)
                positions.Add(representation.ParticipantB.Value);
            foreach (Vector2Int position in positions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (petalViewManager.TryAcquireView(position, nameof(SunburstSkillPresenter), out ViewAccessKey accessKey))
                    accessKeys.Add(position, accessKey);
            }
        }

        protected override async UniTask Play(SunburstRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            UniTask sourceTask;
            Vector2 effectOrigin;
            if (representation.ParticipantB.HasValue)
            {
                Vector2Int participantB = representation.ParticipantB.Value;
                effectOrigin = ((Vector2)representation.ParticipantA + (Vector2)participantB) * 0.5f;
                sourceTask = accessKeys.ContainsKey(representation.ParticipantA) && accessKeys.ContainsKey(participantB)
                    ? UniTask.WhenAll(petalViewManager.PlayComboMerge(representation.ParticipantA, participantB, accessKeys), petalViewManager.PlayComboSpinAndRelease(representation.ParticipantA, participantB, spinDuration, accessKeys))
                    : UniTask.CompletedTask;
            }
            else
            {
                effectOrigin = representation.ParticipantA;
                IReadOnlyList<Vector2Int> source = accessKeys.ContainsKey(representation.ParticipantA) ? new[] { representation.ParticipantA } : Array.Empty<Vector2Int>();
                sourceTask = petalViewManager.PlayDisappearAndRelease(source, spinDuration, accessKeys);
            }

            UniTask affectedPetalTask;
            List<Vector2Int> laserTargets;
            if (representation.ReplacementSkill == SpecialSkillType.None)
            {
                HashSet<Vector2Int> laserTargetPositions = SkillPresentationQueries.GetRemovedPositions(resolution);
                laserTargetPositions.Remove(representation.ParticipantA);
                HashSet<Vector2Int> removedPositions = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(resolution);
                removedPositions.Remove(representation.ParticipantA);
                if (representation.ParticipantB.HasValue)
                {
                    laserTargetPositions.Remove(representation.ParticipantB.Value);
                    removedPositions.Remove(representation.ParticipantB.Value);
                }
                foreach (Vector2Int position in removedPositions)
                {
                    if (accessKeys.ContainsKey(position)) continue;
                    if (petalViewManager.TryAcquireView(position, nameof(SunburstSkillPresenter), out ViewAccessKey accessKey))
                        accessKeys.Add(position, accessKey);
                }
                removedPositions.RemoveWhere(position => !accessKeys.ContainsKey(position));
                var triggeredSkillPositions = new List<Vector2Int>();
                foreach (Vector2Int position in resolution.GetSkillTriggerPositions())
                {
                    if (!accessKeys.ContainsKey(position) && petalViewManager.TryAcquireView(position, nameof(SunburstSkillPresenter), out ViewAccessKey accessKey))
                        accessKeys.Add(position, accessKey);
                    if (accessKeys.ContainsKey(position))
                        triggeredSkillPositions.Add(position);
                }

                laserTargets = new List<Vector2Int>(laserTargetPositions);
                affectedPetalTask = UniTask.WhenAll(petalViewManager.PlayNormalRemovals(new List<Vector2Int>(removedPositions), accessKeys, mutationDuration), petalViewManager.PlayAboutToExecuteShake(triggeredSkillPositions, accessKeys));
            }
            else
            {
                laserTargets = new List<Vector2Int>(representation.Changes.Count);
                var ownedChanges = new List<PetalChange>();
                foreach (PetalChange change in representation.Changes)
                {
                    laserTargets.Add(change.Position);
                    if (!accessKeys.ContainsKey(change.Position) && petalViewManager.TryAcquireView(change.Position, nameof(SunburstSkillPresenter), out ViewAccessKey accessKey))
                        accessKeys.Add(change.Position, accessKey);
                    if (accessKeys.ContainsKey(change.Position))
                        ownedChanges.Add(change);
                }
                affectedPetalTask = petalViewManager.OnPetalsChanged(ownedChanges, layout, mutationDuration, accessKeys);
            }

            UniTask laserTask = boardVFXManager.PlayMutationLaserVFX(effectOrigin, laserTargets, laserChargeUpDuration, laserDuration);
            await UniTask.Delay(TimeSpan.FromSeconds(laserChargeUpDuration));
            var changes = new List<TileChange>();
            foreach (TileChange change in resolution.TileChanges)
            {
                if (change.ObstacleLayerChanged)
                    changes.Add(change);
            }
            UniTask tileTask = tileViewManager.PlayTileChanges(changes);
            await UniTask.WhenAll(sourceTask, affectedPetalTask, tileTask, laserTask);
        }
    }
}
