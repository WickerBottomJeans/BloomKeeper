using BloomKeeper.PlayFabFunctions.Models;
using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Services;

public class LevelAttemptService
{
    /// <summary>
    /// [Duong] Try to start the requested level attempt for the player.
    /// </summary>
    public (StartLevelAttemptResponse response, LevelAttemptData levelAttempt, bool levelAttemptChanged) TryStartLevelAttempt(PlayerProgressionData progression, LevelAttemptData currentLevelAttempt, StartLevelAttemptRequest request, LevelData level)
    {
        //[Duong] safety check
        if (progression == null) throw new ArgumentNullException(nameof(progression));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (!Guid.TryParseExact(request.startLevelRequestIdempotencyKey, "N", out Guid parsedStartLevelRequestIdempotencyKey)) throw new ArgumentException("Start level request idempotency key must be a canonical GUID.", nameof(request));

        string startLevelRequestIdempotencyKey = parsedStartLevelRequestIdempotencyKey.ToString("N");
        //[Duong] Idempotency check
        if (currentLevelAttempt != null && currentLevelAttempt.startLevelRequestIdempotencyKey == startLevelRequestIdempotencyKey)
        {
            if (currentLevelAttempt.levelId != request.levelId)
                return (CreateStartRejectedResponse(StartLevelAttemptRejectionReason.OperationConflict), currentLevelAttempt, false);

            return (CreateStartedResponse(currentLevelAttempt.attemptId), currentLevelAttempt, false);
        }

        //[Duong] Reject if this level is unavailable or still locked
        if (!LevelService.IsLevelAvailable(level)) return (CreateStartRejectedResponse(StartLevelAttemptRejectionReason.LevelUnavailable), currentLevelAttempt, false);
        if (!LevelService.IsLevelUnlocked(progression, level)) return (CreateStartRejectedResponse(StartLevelAttemptRejectionReason.LevelLocked), currentLevelAttempt, false);
        
        // Create active level attempt
        var levelAttempt = new LevelAttemptData
        {
            attemptId = Guid.NewGuid().ToString("N"),
            startLevelRequestIdempotencyKey = startLevelRequestIdempotencyKey,
            levelId = request.levelId,
            status = LevelAttemptStatus.Active
        };
        return (CreateStartedResponse(levelAttempt.attemptId), levelAttempt, true);
    }

    public (AbandonLevelAttemptResponse response, LevelAttemptData levelAttempt, bool levelAttemptChanged) Abandon(LevelAttemptData currentLevelAttempt, AbandonLevelAttemptRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (!Guid.TryParseExact(request.levelAttemptId, "N", out Guid parsedAttemptId)) throw new ArgumentException("Level attempt ID must be a canonical GUID.", nameof(request));

        string attemptId = parsedAttemptId.ToString("N");
        if (currentLevelAttempt == null || currentLevelAttempt.attemptId != attemptId)
            return (CreateAbandonRejectedResponse(AbandonLevelAttemptRejectionReason.AttemptNotCurrent), currentLevelAttempt, false);
        if (currentLevelAttempt.status == LevelAttemptStatus.Completed)
            return (CreateAbandonRejectedResponse(AbandonLevelAttemptRejectionReason.AttemptAlreadyCompleted), currentLevelAttempt, false);
        if (currentLevelAttempt.status == LevelAttemptStatus.Abandoned)
            return (CreateAbandonedResponse(), currentLevelAttempt, false);

        currentLevelAttempt.status = LevelAttemptStatus.Abandoned;
        return (CreateAbandonedResponse(), currentLevelAttempt, true);
    }

    /// <summary>
    /// [Duong] Completes the current level attempt when valid and tells the caller what changed.
    /// </summary>
    public LevelAttemptCompletionResult CompleteLevelAttempt(PlayerProgressionData progression, LevelAttemptData currentLevelAttempt, CompleteLevelAttemptRequest request, LevelData level)
    {
        if (progression == null) throw new ArgumentNullException(nameof(progression));
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (!Guid.TryParseExact(request.attemptId, "N", out Guid parsedAttemptId))
            return new LevelAttemptCompletionResult(CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.InvalidAttemptId), null, null);

        string attemptId = parsedAttemptId.ToString("N");
        if (currentLevelAttempt == null || currentLevelAttempt.attemptId != attemptId)
            return new LevelAttemptCompletionResult(CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptNotCurrent), null, null);
        if (currentLevelAttempt.levelId != request.levelId)
            return new LevelAttemptCompletionResult(CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptLevelMismatch), null, null);
        if (currentLevelAttempt.status == LevelAttemptStatus.Completed)
        {
            if (!MatchesCompletedResult(currentLevelAttempt, request))
                return new LevelAttemptCompletionResult(CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptResultConflict), null, null);

            return new LevelAttemptCompletionResult(CreateCompleteSavedResponse(progression, request.levelId), null, null);
        }
        if (currentLevelAttempt.status != LevelAttemptStatus.Active)
            return new LevelAttemptCompletionResult(CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptNotActive), null, null);

        CompleteLevelAttemptRejectionReason? rejectionReason = GetCompletionRejectionReason(progression, request);
        if (rejectionReason.HasValue)
            return new LevelAttemptCompletionResult(CreateCompleteRejectedResponse(progression, request.levelId, rejectionReason.Value), null, null);

        PlayerProgressionData? updatedProgression = request.didWin ? CreateUpdatedProgression(progression, request, level) : null;
        LevelAttemptData updatedLevelAttempt = CreateCompletedLevelAttempt(currentLevelAttempt, request);
        CompleteLevelAttemptResponse response = CreateCompleteSavedResponse(updatedProgression ?? progression, request.levelId);
        return new LevelAttemptCompletionResult(response, updatedProgression, updatedLevelAttempt);
    }

    private PlayerProgressionData CreateUpdatedProgression(PlayerProgressionData progression, CompleteLevelAttemptRequest request, LevelData level)
    {
        var updatedProgression = new PlayerProgressionData { schemaVersion = progression.schemaVersion, highestUnlockedLevel = progression.highestUnlockedLevel };
        foreach ((int levelId, LevelProgressData levelProgress) in progression.levels)
        {
            updatedProgression.levels.Add(levelId, new LevelProgressData { completed = levelProgress.completed, bestStars = levelProgress.bestStars, bestScore = levelProgress.bestScore });
        }

        updatedProgression.levels.TryGetValue(request.levelId, out LevelProgressData updatedLevelProgress);
        updatedLevelProgress ??= new LevelProgressData();
        updatedLevelProgress.completed = true;
        updatedLevelProgress.bestStars = Math.Max(updatedLevelProgress.bestStars, request.stars);
        updatedLevelProgress.bestScore = Math.Max(updatedLevelProgress.bestScore, request.score);
        int? nextLevelId = LevelService.GetNextLevelId(level, request.levelId);
        if (nextLevelId.HasValue) updatedProgression.highestUnlockedLevel = Math.Max(updatedProgression.highestUnlockedLevel, nextLevelId.Value);
        updatedProgression.levels[request.levelId] = updatedLevelProgress;
        return updatedProgression;
    }

    private LevelAttemptData CreateCompletedLevelAttempt(LevelAttemptData currentLevelAttempt, CompleteLevelAttemptRequest request)
    {
        return new LevelAttemptData
        {
            schemaVersion = currentLevelAttempt.schemaVersion,
            attemptId = currentLevelAttempt.attemptId,
            startLevelRequestIdempotencyKey = currentLevelAttempt.startLevelRequestIdempotencyKey,
            levelId = currentLevelAttempt.levelId,
            status = LevelAttemptStatus.Completed,
            didSpendLife = currentLevelAttempt.didSpendLife,
            didWin = request.didWin,
            score = request.score,
            stars = request.stars
        };
    }

    private  bool MatchesCompletedResult(LevelAttemptData levelAttempt, CompleteLevelAttemptRequest request)
    {
        return levelAttempt.didWin == request.didWin && levelAttempt.score == request.score && levelAttempt.stars == request.stars;
    }

    private  CompleteLevelAttemptRejectionReason? GetCompletionRejectionReason(PlayerProgressionData progression, CompleteLevelAttemptRequest request)
    {
        if (request.stars < 0) return CompleteLevelAttemptRejectionReason.NegativeStars;
        if (request.score < 0) return CompleteLevelAttemptRejectionReason.NegativeScore;
        return null;
    }

    private  StartLevelAttemptResponse CreateStartedResponse(string levelAttemptId)
    {
        return new StartLevelAttemptResponse { schemaVersion = LevelAttemptContract.CurrentSchemaVersion, outcome = StartLevelAttemptOutcome.Approved, levelAttemptId = levelAttemptId };
    }

    public StartLevelAttemptResponse CreateStartRejectedResponse(StartLevelAttemptRejectionReason rejectionReason)
    {
        return new StartLevelAttemptResponse { schemaVersion = LevelAttemptContract.CurrentSchemaVersion, outcome = StartLevelAttemptOutcome.Rejected, rejectionReason = rejectionReason };
    }

    private  AbandonLevelAttemptResponse CreateAbandonedResponse()
    {
        return new AbandonLevelAttemptResponse { schemaVersion = LevelAttemptContract.CurrentSchemaVersion, outcome = AbandonLevelAttemptOutcome.Abandoned };
    }

    private  AbandonLevelAttemptResponse CreateAbandonRejectedResponse(AbandonLevelAttemptRejectionReason rejectionReason)
    {
        return new AbandonLevelAttemptResponse { schemaVersion = LevelAttemptContract.CurrentSchemaVersion, outcome = AbandonLevelAttemptOutcome.Rejected, rejectionReason = rejectionReason };
    }

    private  CompleteLevelAttemptResponse CreateCompleteSavedResponse(PlayerProgressionData progression, int levelId)
    {
        progression.levels.TryGetValue(levelId, out LevelProgressData levelProgress);
        levelProgress ??= new LevelProgressData();
        return new CompleteLevelAttemptResponse { outcome = CompleteLevelAttemptOutcome.Saved, levelId = levelId, levelProgress = levelProgress, highestUnlockedLevel = progression.highestUnlockedLevel };
    }

    private  CompleteLevelAttemptResponse CreateCompleteRejectedResponse(PlayerProgressionData progression, int levelId, CompleteLevelAttemptRejectionReason rejectionReason)
    {
        return new CompleteLevelAttemptResponse { outcome = CompleteLevelAttemptOutcome.Rejected, rejectionReason = rejectionReason, levelId = levelId, highestUnlockedLevel = progression.highestUnlockedLevel };
    }
}
