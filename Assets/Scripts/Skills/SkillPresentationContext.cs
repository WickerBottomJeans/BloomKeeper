using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public class SkillPresentationContext
    {
        private readonly ResolutionPlayback playback;
        private readonly Dictionary<Vector2Int, List<TileChange>> impactTileChangesByPosition = new();
        private readonly Dictionary<Vector2Int, List<TileChange>> inputTileChangesByPosition = new();

        public BoardChangeGroup BoardChangeGroup { get; }
        public PieceViewScope Views { get; }
        public AudioPlaybackScope AudioPlaybackScope { get; }

        public SkillPresentationContext(BoardChangeGroup effect, BoardChangeGroup consumedInputs, ResolutionPlayback playback, PieceViewScope views, AudioPlaybackScope audioPlaybackScope)
        {
            BoardChangeGroup = effect;
            Views = views;
            AudioPlaybackScope = audioPlaybackScope;
            this.playback = playback;
            IndexTileChanges(effect, impactTileChangesByPosition);
            if (consumedInputs != null)
                IndexTileChanges(consumedInputs, inputTileChangesByPosition);
        }

        public async UniTask ClearImmediately(PieceViewManager pieceViewManager, Vector2Int? source = null)
        {
            HashSet<Vector2Int> removed = SkillPresentationQueries.GetRemovedPetalPositionsExcludingTriggeredSkills(BoardChangeGroup);
            if (source.HasValue) removed.Add(source.Value);
            List<Vector2Int> ownedRemoved = Views.AcquireAvailable(removed);
            List<Vector2Int> triggered = Views.AcquireAvailable(BoardChangeGroup.TriggeredSkillPositions);
            pieceViewManager.ReleasePieceViewsImmediately(ownedRemoved, Views);
            await PetalPresentation.PlayAboutToExecute(pieceViewManager, triggered, Views);
            foreach (TileChange change in BoardChangeGroup.TileChanges) HandleTileImpactPresented(change.Position);
        }
        public void HandleTileImpactPresented(Vector2Int position)
        {
            if (!impactTileChangesByPosition.TryGetValue(position, out List<TileChange> tileChanges))
                throw new InvalidOperationException($"This skill has no direct impact at {position}.");
            foreach (TileChange tileChange in tileChanges)
                playback.HandleTileChangePresented(tileChange);
        }

        /// <summary>
        /// Presents input clears that belong to this skill combination.
        /// </summary>
        public void HandleConsumedInputPresented(Vector2Int position)
        {
            if (!inputTileChangesByPosition.TryGetValue(position, out List<TileChange> tileChanges)) return;
            foreach (TileChange tileChange in tileChanges)
                playback.HandleTileChangePresented(tileChange);
        }

        private void IndexTileChanges(BoardChangeGroup effect, Dictionary<Vector2Int, List<TileChange>> tileChangesByPosition)
        {
            foreach (TileChange tileChange in effect.TileChanges)
            {
                Vector2Int position = tileChange.Position;
                if (!tileChangesByPosition.TryGetValue(position, out List<TileChange> tileChanges))
                {
                    tileChanges = new List<TileChange>();
                    tileChangesByPosition.Add(position, tileChanges);
                }
                tileChanges.Add(tileChange);
            }
        }
    }
}
