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
        [Header("Bomb Timing")]
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
        private TileViewManager tileViewManager;
        private BoardVFXManager boardVFXManager;
        private BoardLayout layout;
        private Tile[,] grid;

        public void Init(PetalViewManager petalViewManager, TileViewManager tileViewManager,
            BoardVFXManager boardVFXManager, BoardLayout layout, Tile[,] grid)
        {
            this.petalViewManager = petalViewManager;
            this.tileViewManager = tileViewManager;
            this.boardVFXManager = boardVFXManager;
            this.layout = layout;
            this.grid = grid;
        }

        public UniTask Play(SkillUseResult skillResult, MatchGroupResolveResult resolution)
        {
            return Play(skillResult.Representation, resolution);
        }

        private async UniTask Play(SkillRepresentationData representation, MatchGroupResolveResult resolution)
        {
            switch (representation)
            {
                case null:
                    return;
                case BouquetRepresentationData Bomb:
                    await PlayBouquet(Bomb, resolution);
                    return;
                case StripedRepresentationData striped:
                    await PlayStriped(striped, resolution);
                    return;
                case ButterflyRepresentationData butterfly:
                    await PlayButterfly(butterfly, resolution);
                    return;
                case SunburstComboRepresentationData sunburstCombo:
                    await PlaySunburst(sunburstCombo, resolution);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(representation),
                        representation.GetType(),
                        "Skill representation is not supported.");
            }
        }

        private async UniTask PlayBouquet(BouquetRepresentationData representation, MatchGroupResolveResult resolution)
        {
            HashSet<Vector2Int> removedPositions = GetCurrentSkillConsumedPositions(resolution);
            removedPositions.Add(representation.Center);
            UniTask disappearTask = petalViewManager.PlayDisappearAndRelease(
                new List<Vector2Int>(removedPositions),
                bouquetDisappearDuration);

            UniTask triggeredSkillTask = petalViewManager.PlayAboutToExecuteShake(resolution.TriggeredSkillPositions);
            UniTask bloomTask = boardVFXManager.PlayBouquetBloomVFX(representation.Center);
            UniTask tileTask = PlayTileChanges(resolution);

            await UniTask.WhenAll(disappearTask, triggeredSkillTask, bloomTask, tileTask);
        }

        private async UniTask PlayStriped(StripedRepresentationData representation,
            MatchGroupResolveResult resolution)
        {
            bool isVertical = representation.Direction == SpecialSkillType.StripedVertical;
            HashSet<Vector2Int> removedPositions = GetCurrentSkillConsumedPositions(resolution);
            HashSet<Vector2Int> changedPositions = GetChangedPositions(resolution);
            var patternPositions = new HashSet<Vector2Int> { representation.Source };
            removedPositions.Add(representation.Source);

            foreach (var impact in resolution.Impacts)
                patternPositions.Add(impact.Position);

            int maxDistance = 0;
            foreach (Vector2Int position in patternPositions)
            {
                int distance = isVertical
                    ? Mathf.Abs(position.y - representation.Source.y)
                    : Mathf.Abs(position.x - representation.Source.x);
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            float stepDuration = maxDistance > 0
                ? stripedPropagationDuration / maxDistance
                : stripedPropagationDuration;
            var tasks = new List<UniTask>
            {
                boardVFXManager.PlayStripedSkillVFX(
                    representation.Source, isVertical, stripedPropagationDuration)
            };

            for (int distance = 0; distance <= maxDistance; distance++)
            {
                var petalWave = new List<Vector2Int>();
                var tileWave = new List<Vector2Int>();
                var triggeredSkillWave = new List<Vector2Int>();
                int directionCount = distance == 0 ? 1 : 2;

                for (int direction = 0; direction < directionCount; direction++)
                {
                    int offset = direction == 0 ? distance : -distance;
                    Vector2Int position = isVertical
                        ? new Vector2Int(representation.Source.x, representation.Source.y + offset)
                        : new Vector2Int(representation.Source.x + offset, representation.Source.y);

                    if (removedPositions.Contains(position)) petalWave.Add(position);
                    if (changedPositions.Contains(position)) tileWave.Add(position);
                    if (resolution.IsTriggeredSkillPosition(position)) triggeredSkillWave.Add(position);
                }

                tasks.Add(petalViewManager.PlayDisappearAndRelease(petalWave, stepDuration));
                tasks.Add(petalViewManager.PlayAboutToExecuteShake(triggeredSkillWave));
                tasks.Add(tileViewManager.PlayTileChanges(tileWave, grid));

                if (distance < maxDistance)
                    await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask PlayButterfly(ButterflyRepresentationData representation, MatchGroupResolveResult resolution)
        {
            if (!representation.Target.HasValue)
            {
                await UniTask.WhenAll(
                    petalViewManager.PlayDisappearAndRelease(
                        new[] { representation.Source }, butterflyDisappearDuration),
                    PlayTileChanges(resolution));
                return;
            }

            await petalViewManager.PlayFly(representation.Source, representation.Target.Value, layout, butterflyFlightDuration);
            var disappearingPositions = new List<Vector2Int> { representation.Source };
            var triggeredSkillPositions = new List<Vector2Int>();
            if (WasPetalRemovedAt(resolution, representation.Target.Value) &&
                representation.Target.Value != representation.Source &&
                !resolution.IsTriggeredSkillPosition(representation.Target.Value))
                disappearingPositions.Add(representation.Target.Value);
            if (resolution.IsTriggeredSkillPosition(representation.Target.Value) &&
                representation.Target.Value != representation.Source)
                triggeredSkillPositions.Add(representation.Target.Value);
            await UniTask.WhenAll(
                petalViewManager.PlayDisappearAndRelease(disappearingPositions, butterflyDisappearDuration),
                petalViewManager.PlayAboutToExecuteShake(triggeredSkillPositions),
                PlayTileChanges(resolution));
        }

        private async UniTask PlaySunburst(SunburstComboRepresentationData representation, MatchGroupResolveResult resolution)
        {
            UniTask mergeTask = petalViewManager.PlayComboMerge(representation.SourceA, representation.SourceB);

            UniTask spinTask = petalViewManager.PlayComboSpinAndRelease(
                representation.SourceA,
                representation.SourceB,
                stripeSunburstSpinDuration);

            UniTask affectedPetalTask;
            List<Vector2Int> laserTargets;
            
            //If is SunBurst + Normal petal
            if (representation.ComboSkillType == SpecialSkillType.Sunburst)
            {
                HashSet<Vector2Int> laserTargetPositions = GetRemovedPositions(resolution);
                laserTargetPositions.Remove(representation.SourceA);
                laserTargetPositions.Remove(representation.SourceB);
                HashSet<Vector2Int> removedPositions = GetCurrentSkillConsumedPositions(resolution);
                removedPositions.Remove(representation.SourceA);
                removedPositions.Remove(representation.SourceB);
                laserTargets = new List<Vector2Int>(laserTargetPositions);
                affectedPetalTask = UniTask.WhenAll(
                    petalViewManager.PlayNormalRemovals(new List<Vector2Int>(removedPositions), stripeSunburstMutationDuration),
                    petalViewManager.PlayAboutToExecuteShake(resolution.TriggeredSkillPositions));
            }
            else
            {
                laserTargets = new List<Vector2Int>(representation.Changes.Count);
                foreach (PetalChange change in representation.Changes)
                    laserTargets.Add(change.Position);
                affectedPetalTask = petalViewManager.OnPetalsChanged(
                    representation.Changes,
                    layout,
                    stripeSunburstMutationDuration);
            }

            UniTask laserTask = boardVFXManager.PlayMutationLaserVFX(
                representation.Origin,
                laserTargets,
                stripeSunburstLaserChargeUpDuration,
                stripeSunburstLaserDuration);

            await UniTask.Delay(TimeSpan.FromSeconds(stripeSunburstLaserChargeUpDuration));

            UniTask tileTask = PlayTileChanges(resolution);

            await UniTask.WhenAll(
                mergeTask,
                spinTask,
                affectedPetalTask,
                tileTask,
                laserTask);
        }

        private static HashSet<Vector2Int> GetRemovedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Outcome.RemovedPetal != null)
                    positions.Add(impact.Position);
            }
            return positions;
        }

        private static HashSet<Vector2Int> GetCurrentSkillConsumedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Outcome.RemovedPetal != null && !resolution.IsTriggeredSkillPosition(impact.Position))
                    positions.Add(impact.Position);
            }
            return positions;
        }

        private static HashSet<Vector2Int> GetChangedPositions(MatchGroupResolveResult resolution)
        {
            var positions = new HashSet<Vector2Int>();
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Outcome.TileChanged)
                    positions.Add(impact.Position);
            }
            return positions;
        }

        private static bool WasPetalRemovedAt(MatchGroupResolveResult resolution, Vector2Int position)
        {
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Position == position && impact.Outcome.RemovedPetal != null)
                    return true;
            }
            return false;
        }

        private UniTask PlayTileChanges(MatchGroupResolveResult resolution)
        {
            var changedPositions = new List<Vector2Int>();
            foreach (var impact in resolution.Impacts)
            {
                if (impact.Outcome.TileChanged)
                    changedPositions.Add(impact.Position);
            }
            return tileViewManager.PlayTileChanges(changedPositions, grid);
        }
    }
}
