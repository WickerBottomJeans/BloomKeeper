using System;
using Skills;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using UnityEngine;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// Animates one BoardResolution, including matches, skills, and dependent tile reactions.
    /// Each instance lasts for that single playback. Gravity, refill, further cascade
    /// resolutions, and the return to Idle are sequenced separately by GameBoard.
    /// </summary>
    public class ResolutionPlayback : IDisposable
    {
        // Dependencies
        private readonly TileViewManager tileViewManager;
        private readonly PieceViewManager pieceViewManager;
        private readonly SkillAnimationLibrary skillAnimationLibrary;
        private readonly BoardLayout boardLayout;
        private readonly BoardAudioManager boardAudioManager;

        // Input
        private readonly BoardResolution _boardResolution;

        // Change tracking
        private readonly Dictionary<TileChange, List<TileChange>> dependentTileChanges = new();
        private readonly Dictionary<TileChange, UniTaskCompletionSource> completionSources = new();
        private readonly Dictionary<TileChange, UniTaskCompletionSource> impactSources = new();
        private readonly HashSet<TileChange> presentedTileChanges = new();
        private readonly HashSet<TileChange> readyTileChanges = new();

        // Feature queues
        private readonly Dictionary<Vector2Int, Queue<TileChange>> featureQueues = new();
        private readonly HashSet<Vector2Int> animatingFeaturePositions = new();
        private bool featureClearAudioPlayed;

        // Cancellation and failure
        private readonly CancellationTokenSource presentationCancellationTokenSource;
        private readonly CancellationTokenRegistration cancellationTokenRegistration;
        private Exception presentationFailure;

        public CancellationToken CancellationToken => presentationCancellationTokenSource.Token;

        public ResolutionPlayback(BoardResolution boardResolution, PieceViewManager pieceViewManager, TileViewManager tileViewManager, BoardAudioManager boardAudioManager, SkillAnimationLibrary skillAnimationLibrary, BoardLayout boardLayout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Init
            this._boardResolution = boardResolution;
            this.pieceViewManager = pieceViewManager;
            this.skillAnimationLibrary = skillAnimationLibrary;
            this.boardLayout = boardLayout;
            this.tileViewManager = tileViewManager;
            this.boardAudioManager = boardAudioManager;

            // Register changes and triggers
            RegisterResolution(boardResolution);
            foreach (ResolvedSkill resolvedSkill in boardResolution.SkillResults)
            {
                if (resolvedSkill.Trigger == null) continue;
                if (!completionSources.ContainsKey(resolvedSkill.Trigger) || resolvedSkill.Trigger.ImmediateCause != null || !resolvedSkill.Trigger.HasAnyChange)
                    throw new InvalidOperationException("A triggered skill requires a direct change from this boardResolution.");
                if (!impactSources.ContainsKey(resolvedSkill.Trigger))
                    impactSources.Add(resolvedSkill.Trigger, new UniTaskCompletionSource());
            }

            // Cancellation setup
            presentationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenRegistration = presentationCancellationTokenSource.Token.Register(() => FailPresentation(new OperationCanceledException(presentationCancellationTokenSource.Token)));
        }

        public async UniTask Play()
        {
            // Prep
            using var audioScope = new AudioPlaybackScope();
            using var normalViews = new PieceViewScope(pieceViewManager, "Normal clears");
            var skillPlaybacks = CreateSkillPlaybacks(audioScope);
            var normalTileEffects = GetNormalEffects();
            var normalEffectsCompleted = new UniTaskCompletionSource();
            var presentationTasks = new List<UniTask>();

            try
            {
                // Acquire initial views
                foreach (SkillPlayback skillPlayback in skillPlaybacks)
                    if (skillPlayback.Resolution.Trigger == null && !skillPlayback.WaitForCreation) skillPlayback.AcquireViews();
                AcquireSkillPetalSpawnViews(_boardResolution.SpawnedPetals, normalViews);
                foreach (BoardChangeGroup effect in normalTileEffects) AcquireViews(effect, normalViews);

                // Start playback
                foreach (SkillPlayback skillPlayback in skillPlaybacks) presentationTasks.Add(PlaySkill(skillPlayback, normalEffectsCompleted));
                presentationTasks.Add(PlayNormalEffects(normalTileEffects, normalViews, normalEffectsCompleted));
                presentationTasks.Add(PlayEggCollections());

                // Each task reports failure without throwing, so all running animations drain before cleanup.
                await UniTask.WhenAll(presentationTasks);
                await Complete();
            }
            catch (Exception exception)
            {
                FailPresentation(exception);
                throw;
            }
            finally
            {
                // Cleanup
                foreach (SkillPlayback skillPlayback in skillPlaybacks) skillPlayback.Context.Views.Dispose();
            }
        }

        private async UniTask PlayEggCollections()
        {
            try
            {
                var presenter = new EggCollectionPresenter(pieceViewManager, tileViewManager, boardLayout);
                await presenter.Play(_boardResolution.EggCollections, this);
            }
            catch (Exception exception)
            {
                FailPresentation(exception);
            }
        }

        private List<SkillPlayback> CreateSkillPlaybacks(AudioPlaybackScope audioScope)
        {
            // Prep
            var spawnedPetalPositions = new HashSet<Vector2Int>();
            foreach (SkillPetalSpawn skillPetalSpawn in _boardResolution.SpawnedPetals) spawnedPetalPositions.Add(skillPetalSpawn.SpawnPosition);

            // Build skill playbacks
            var skillPlaybacks = new List<SkillPlayback>();
            foreach (ResolvedSkill resolvedSkill in _boardResolution.SkillResults)
            {
                ISkillAnimation skillAnimation = skillAnimationLibrary.GetPresenter(resolvedSkill.skillPlan.Representation);
                var requiredViews = new HashSet<Vector2Int>(skillAnimation.RequiredViews(resolvedSkill.skillPlan.Representation, resolvedSkill.Effect));
                var context = new SkillPresentationContext(resolvedSkill.Effect, resolvedSkill.ConsumedInputs, this, new PieceViewScope(pieceViewManager, skillAnimation.GetType().Name), audioScope);
                skillPlaybacks.Add(new SkillPlayback(resolvedSkill, skillAnimation, context, requiredViews, requiredViews.Overlaps(spawnedPetalPositions)));
            }
            return skillPlaybacks;
        }

        private List<BoardChangeGroup> GetNormalEffects()
        {
            // Filter out skill effects
            var skillEffects = new HashSet<BoardChangeGroup>();
            foreach (ResolvedSkill resolvedSkill in _boardResolution.SkillResults)
            {
                skillEffects.Add(resolvedSkill.Effect);
                if (resolvedSkill.ConsumedInputs != null) skillEffects.Add(resolvedSkill.ConsumedInputs);
            }

            var normalTileEffects = new List<BoardChangeGroup>();
            foreach (BoardChangeGroup effect in _boardResolution.Effects)
                if (!skillEffects.Contains(effect)) normalTileEffects.Add(effect);
            return normalTileEffects;
        }

        private async UniTask PlaySkill(SkillPlayback skillPlayback, UniTaskCompletionSource normalEffectsCompleted)
        {
            try
            {
                // Wait for dependencies
                CancellationToken.ThrowIfCancellationRequested();
                if (skillPlayback.Resolution.Trigger != null) await WaitForImpact(skillPlayback.Resolution.Trigger);
                if (skillPlayback.WaitForCreation) await normalEffectsCompleted.Task.AttachExternalCancellation(CancellationToken);
                CancellationToken.ThrowIfCancellationRequested();
                if (skillPlayback.Resolution.Trigger != null || skillPlayback.WaitForCreation) skillPlayback.AcquireViews();

                // Animate and validate
                await skillPlayback.Animator.Play(skillPlayback.Resolution.skillPlan.Representation, skillPlayback.Context);
                ValidateDirectChanges(skillPlayback.Resolution.Effect.TileChanges);
                if (skillPlayback.Resolution.ConsumedInputs != null) ValidateDirectChanges(skillPlayback.Resolution.ConsumedInputs.TileChanges);
            }
            catch (Exception exception)
            {
                FailPresentation(exception);
            }
        }

        private async UniTask PlayNormalEffects(IReadOnlyList<BoardChangeGroup> tileEffects, PieceViewScope petalViewScope, UniTaskCompletionSource effectsCompleted)
        {
            try
            {
                CancellationToken.ThrowIfCancellationRequested();

                // Animate and signal completion
                await PlayMatches(tileEffects, _boardResolution.SpawnedPetals, petalViewScope);
                foreach (BoardChangeGroup effect in tileEffects) ValidateDirectChanges(effect.TileChanges);
                effectsCompleted.TrySetResult();
            }
            catch (Exception exception)
            {
                FailPresentation(exception);
            }
        }

        private sealed class SkillPlayback
        {
            public ResolvedSkill Resolution { get; }
            public ISkillAnimation Animator { get; }
            public SkillPresentationContext Context { get; }
            public bool WaitForCreation { get; }
            private readonly IEnumerable<Vector2Int> requiredViews;

            public SkillPlayback(ResolvedSkill resolvedSkill, ISkillAnimation skillAnimation, SkillPresentationContext context, IEnumerable<Vector2Int> requiredViews, bool waitForCreation)
            {
                Resolution = resolvedSkill;
                Animator = skillAnimation;
                Context = context;
                this.requiredViews = requiredViews;
                WaitForCreation = waitForCreation;
            }

            public void AcquireViews()
            {
                // Transfer view ownership
                if (Resolution.Trigger != null)
                    foreach (Vector2Int position in Resolution.skillPlan.Representation.ConsumedInputPositions) Context.Views.TakeOver(position);
                foreach (Vector2Int position in requiredViews) Context.Views.AcquireRequired(position);
            }
        }
        private void AcquireSkillPetalSpawnViews(IReadOnlyList<SkillPetalSpawn> skillPetalSpawns, PieceViewScope petalViewScope)
        {
            foreach (SkillPetalSpawn skillPetalSpawn in skillPetalSpawns)
            {
                foreach (Vector2Int position in skillPetalSpawn.ContributorPositions)
                {
                    if (petalViewScope.Owns(position)) continue;
                    petalViewScope.TryAcquire(position);
                }
            }
        }

        private void AcquireViews(BoardChangeGroup groupResult, PieceViewScope petalViewScope)
        {
            var positions = new List<Vector2Int>();
            AddNormalRemovals(groupResult, positions);
            foreach (Vector2Int position in positions)
            {
                if (petalViewScope.Owns(position)) continue;
                petalViewScope.TryAcquire(position);
            }
        }

        private async UniTask PlayMatches(IReadOnlyList<BoardChangeGroup> groupResults, IReadOnlyList<SkillPetalSpawn> skillPetalSpawns, PieceViewScope petalViewScope)
        {
            // Prep
            var normalRemovals = new List<Vector2Int>();
            var presentationTasks = new List<UniTask>();

            foreach (BoardChangeGroup groupResult in groupResults)
            {
                AddNormalRemovals(groupResult, normalRemovals);
            }

            if (normalRemovals.Count > 0)
                boardAudioManager.PlayMatchClear();

            // Contributors use the skill creation animation instead of an ordinary removal.
            var skillCreationPositions = new HashSet<Vector2Int>();
            foreach (SkillPetalSpawn skillPetalSpawn in skillPetalSpawns)
            {
                foreach (Vector2Int position in skillPetalSpawn.ContributorPositions)
                    skillCreationPositions.Add(position);
            }

            normalRemovals.RemoveAll(skillCreationPositions.Contains);
            normalRemovals.RemoveAll(position => !petalViewScope.Owns(position));

            // Animate
            presentationTasks.Add(pieceViewManager.PlayNormalRemovals(normalRemovals, petalViewScope));
            presentationTasks.Add(PetalPresentation.PlaySkillPetalCreations(pieceViewManager, skillPetalSpawns, boardLayout, petalViewScope));

            // Report impacts now so reactions can overlap the finishing petal animations.
            foreach (BoardChangeGroup groupResult in groupResults)
                foreach (TileChange tileChange in groupResult.TileChanges)
                    HandleTileChangePresented(tileChange);
            await UniTask.WhenAll(presentationTasks);
        }

        private static void AddNormalRemovals(BoardChangeGroup groupResult, List<Vector2Int> normalRemovals)
        {

            foreach (var impact in groupResult.TileChanges)
            {
                if (!impact.PetalWasRemoved || impact.RemovedSkillType != SpecialSkillType.None) continue;
                normalRemovals.Add(impact.Position);
            }
        }

        /// <summary>
        /// Releases a direct change and the reactions caused by its presentation.
        /// </summary>
        public void HandleTileChangePresented(TileChange tileChange)
        {
            // Guards
            ThrowIfPresentationFailed();
            if (!completionSources.ContainsKey(tileChange)) throw new InvalidOperationException("The tile change does not belong to this turn presentation.");
            if (tileChange.ImmediateCause != null) throw new InvalidOperationException("A reaction must be released by its immediate cause.");
            if (!presentedTileChanges.Add(tileChange)) throw new InvalidOperationException($"The tile change at {tileChange.Position} was presented twice.");

            // Release impact
            if (impactSources.TryGetValue(tileChange, out UniTaskCompletionSource impactSource))
                impactSource.TrySetResult();
            if (tileChange.HasAnyChange)
                ReleaseTileChange(tileChange);
        }

        public UniTask WaitForImpact(TileChange trigger)
        {
            ThrowIfPresentationFailed();
            return impactSources[trigger].Task;
        }

        public void ValidateDirectChanges(IReadOnlyList<TileChange> tileChanges)
        {
            ThrowIfPresentationFailed();
            foreach (TileChange change in tileChanges)
                if (change.ImmediateCause == null && change.HasAnyChange && !presentedTileChanges.Contains(change))
                    throw new InvalidOperationException($"The direct tile change at {change.Position} has no presentation signal.");
        }

        /// <summary>
        /// Checks direct submissions and waits for this boardResolution's reactions.
        /// </summary>
        public async UniTask Complete()
        {
            try
            {
                // Validate signals
                ThrowIfPresentationFailed();
                ValidateDirectChanges(_boardResolution.TileChanges);

                // Wait for completion
                var presentationTasks = new List<UniTask>(_boardResolution.TileChanges.Count);
                foreach (TileChange tileChange in _boardResolution.TileChanges)
                {
                    presentationTasks.Add(completionSources[tileChange].Task);
                }
                await UniTask.WhenAll(presentationTasks);
            }
            catch (Exception exception)
            {
                FailPresentation(exception);
                throw;
            }
        }

        public void FailPresentation(Exception exception)
        {
            // Record failure and cancel
            if (presentationFailure != null) return;
            presentationFailure = exception;
            presentationCancellationTokenSource.Cancel();

            // Unblock waiters
            foreach (UniTaskCompletionSource impactSource in impactSources.Values)
            {
                if (exception is OperationCanceledException cancellationException)
                    impactSource.TrySetCanceled(cancellationException.CancellationToken);
                else
                    impactSource.TrySetException(exception);
            }
            foreach (UniTaskCompletionSource completionSource in completionSources.Values)
            {
                if (exception is OperationCanceledException cancellationException)
                    completionSource.TrySetCanceled(cancellationException.CancellationToken);
                else
                    completionSource.TrySetException(exception);
            }
        }

        public void Dispose()
        {
            cancellationTokenRegistration.Dispose();
            FailPresentation(new OperationCanceledException(presentationCancellationTokenSource.Token));
            presentationCancellationTokenSource.Dispose();
        }

        private void RegisterResolution(BoardResolution boardResolution)
        {
            foreach (TileChange tileChange in boardResolution.TileChanges)
            {
                // Register dependencies
                TileChange immediateCause = tileChange.ImmediateCause;
                if (immediateCause != null)
                {
                    if (!completionSources.ContainsKey(immediateCause) || !immediateCause.HasAnyChange)
                        throw new InvalidOperationException("A reaction requires an earlier change in the turn.");
                    if (!dependentTileChanges.TryGetValue(immediateCause, out List<TileChange> causedTileChanges))
                    {
                        causedTileChanges = new List<TileChange>();
                        dependentTileChanges.Add(immediateCause, causedTileChanges);
                    }
                    causedTileChanges.Add(tileChange);
                }

                // Register completion
                var completionSource = new UniTaskCompletionSource();
                completionSources.Add(tileChange, completionSource);
                if (!tileChange.HasAnyChange)
                    completionSource.TrySetResult();

                // Queue feature changes
                if (!tileChange.FeatureChanged) continue;
                Vector2Int position = tileChange.Position;
                if (!featureQueues.TryGetValue(position, out Queue<TileChange> featureQueue))
                {
                    featureQueue = new Queue<TileChange>();
                    featureQueues.Add(position, featureQueue);
                }
                featureQueue.Enqueue(tileChange);
            }
        }

        private void ReleaseTileChange(TileChange tileChange)
        {
            if (!readyTileChanges.Add(tileChange)) throw new InvalidOperationException("A tile change was released twice.");

            // Animate or complete
            if (tileChange.FeatureChanged)
                PlayFeatureQueue(tileChange.Position).Forget();
            else
                CompleteTileChange(tileChange);
        }

        private async UniTask PlayFeatureQueue(Vector2Int position)
        {
            // Prevent overlapping queue playback
            if (!animatingFeaturePositions.Add(position)) return;

            try
            {
                // Stop at the first transition whose cause has not released it yet.
                Queue<TileChange> featureQueue = featureQueues[position];
                while (featureQueue.Count > 0 && readyTileChanges.Contains(featureQueue.Peek()))
                {
                    ThrowIfPresentationFailed();
                    TileChange tileChange = featureQueue.Peek();

                    // Animate
                    UniTask featureTask = tileViewManager.PlayTileChange(tileChange, presentationCancellationTokenSource.Token);
                    if (tileChange.FeatureWasRemoved && !featureClearAudioPlayed)
                    {
                        featureClearAudioPlayed = true;
                        boardAudioManager.PlayFeatureCleared(tileChange.Before.FeatureState.FeatureType);
                    }
                    await featureTask;
                    ThrowIfPresentationFailed();

                    // Advance queue
                    featureQueue.Dequeue();
                    CompleteTileChange(tileChange);
                }
            }
            catch (Exception exception)
            {
                FailPresentation(exception);
            }
            finally
            {
                animatingFeaturePositions.Remove(position);
            }
        }

        private void CompleteTileChange(TileChange tileChange)
        {
            // Release reactions and complete
            if (dependentTileChanges.TryGetValue(tileChange, out List<TileChange> causedTileChanges))
                foreach (TileChange causedTileChange in causedTileChanges)
                    ReleaseTileChange(causedTileChange);
            completionSources[tileChange].TrySetResult();
        }

        private void ThrowIfPresentationFailed()
        {
            if (presentationFailure is OperationCanceledException cancellationException) throw cancellationException;
            if (presentationFailure != null) throw new InvalidOperationException("Board change presentation has failed.", presentationFailure);
            presentationCancellationTokenSource.Token.ThrowIfCancellationRequested();
        }
    }
}
