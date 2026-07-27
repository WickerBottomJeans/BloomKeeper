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
        private readonly BoardCell[,] grid;
        private readonly float spinDuration;
        private readonly float mutationDuration;
        private readonly float laserDuration;
        private readonly float laserChargeUpDuration;

        public SunburstSkillPresenter(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardLayout layout, BoardCell[,] grid, float spinDuration, float mutationDuration, float laserDuration, float laserChargeUpDuration)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
            this.grid = grid;
            this.spinDuration = spinDuration;
            this.mutationDuration = mutationDuration;
            this.laserDuration = laserDuration;
            this.laserChargeUpDuration = laserChargeUpDuration;
        }

        protected override async UniTask Play(SunburstRepresentationData representation, MatchGroupResolveResult resolution)
        {
            UniTask sourceTask;
            Vector2 effectOrigin;
            if (representation.ParticipantB.HasValue)
            {
                Vector2Int participantB = representation.ParticipantB.Value;
                effectOrigin = ((Vector2)representation.ParticipantA + (Vector2)participantB) * 0.5f;
                sourceTask = UniTask.WhenAll(petalViewManager.PlayComboMerge(representation.ParticipantA, participantB), petalViewManager.PlayComboSpinAndRelease(representation.ParticipantA, participantB, spinDuration));
            }
            else
            {
                effectOrigin = representation.ParticipantA;
                sourceTask = petalViewManager.PlayDisappearAndRelease(new[] { representation.ParticipantA }, spinDuration);
            }

            UniTask affectedPetalTask;
            List<Vector2Int> laserTargets;
            if (representation.ReplacementSkill == SpecialSkillType.None)
            {
                HashSet<Vector2Int> laserTargetPositions = SkillPresentationImpactQueries.GetRemovedPositions(resolution);
                laserTargetPositions.Remove(representation.ParticipantA);
                HashSet<Vector2Int> removedPositions = SkillPresentationImpactQueries.GetCurrentSkillConsumedPositions(resolution);
                removedPositions.Remove(representation.ParticipantA);
                if (representation.ParticipantB.HasValue)
                {
                    laserTargetPositions.Remove(representation.ParticipantB.Value);
                    removedPositions.Remove(representation.ParticipantB.Value);
                }

                laserTargets = new List<Vector2Int>(laserTargetPositions);
                affectedPetalTask = UniTask.WhenAll(petalViewManager.PlayNormalRemovals(new List<Vector2Int>(removedPositions), mutationDuration), petalViewManager.PlayAboutToExecuteShake(resolution.TriggeredSkillPositions));
            }
            else
            {
                laserTargets = new List<Vector2Int>(representation.Changes.Count);
                foreach (PetalChange change in representation.Changes)
                    laserTargets.Add(change.Position);
                affectedPetalTask = petalViewManager.OnPetalsChanged(representation.Changes, layout, mutationDuration);
            }

            UniTask laserTask = boardVFXManager.PlayMutationLaserVFX(effectOrigin, laserTargets, laserChargeUpDuration, laserDuration);
            await UniTask.Delay(TimeSpan.FromSeconds(laserChargeUpDuration));
            var changedPositions = new List<Vector2Int>(SkillPresentationImpactQueries.GetChangedPositions(resolution));
            UniTask tileTask = tileViewManager.PlayTileChanges(changedPositions, grid);
            await UniTask.WhenAll(sourceTask, affectedPetalTask, tileTask, laserTask);
        }
    }
}
