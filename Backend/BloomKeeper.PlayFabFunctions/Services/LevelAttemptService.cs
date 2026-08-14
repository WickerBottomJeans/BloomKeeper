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

    public (CompleteLevelAttemptResponse response, bool progressionChanged, bool levelAttemptChanged) Complete(PlayerProgressionData progression, LevelAttemptData currentLevelAttempt, CompleteLevelAttemptRequest request, LevelData level)
    {
        if (progression == null) throw new ArgumentNullException(nameof(progression));
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (!Guid.TryParseExact(request.attemptId, "N", out Guid parsedAttemptId))
            return (CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.InvalidAttemptId), false, false);

        string attemptId = parsedAttemptId.ToString("N");
        if (currentLevelAttempt == null || currentLevelAttempt.attemptId != attemptId)
            return (CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptNotCurrent), false, false);
        if (currentLevelAttempt.levelId != request.levelId)
            return (CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptLevelMismatch), false, false);
        if (currentLevelAttempt.status == LevelAttemptStatus.Completed)
        {
            if (!MatchesCompletedResult(currentLevelAttempt, request))
                return (CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptResultConflict), false, false);

            return (CreateCompleteSavedResponse(progression, request.levelId), false, false);
        }
        if (currentLevelAttempt.status != LevelAttemptStatus.Active)
            return (CreateCompleteRejectedResponse(progression, request.levelId, CompleteLevelAttemptRejectionReason.AttemptNotActive), false, false);

        CompleteLevelAttemptRejectionReason? rejectionReason = GetCompletionRejectionReason(progression, request);
        if (rejectionReason.HasValue)
            return (CreateCompleteRejectedResponse(progression, request.levelId, rejectionReason.Value), false, false);

        bool progressionChanged = ApplyProgression(progression, request, level);
        currentLevelAttempt.status = LevelAttemptStatus.Completed;
        currentLevelAttempt.didWin = request.didWin;
        currentLevelAttempt.score = request.score;
        currentLevelAttempt.stars = request.stars;
        return (CreateCompleteSavedResponse(progression, request.levelId), progressionChanged, true);
    }

    private  bool ApplyProgression(PlayerProgressionData progression, CompleteLevelAttemptRequest request, LevelData level)
    {
        if (!request.didWin) return false;

        progression.levels.TryGetValue(request.levelId, out LevelProgressData levelProgress);
        levelProgress ??= new LevelProgressData();
        levelProgress.completed = true;
        levelProgress.bestStars = Math.Max(levelProgress.bestStars, request.stars);
        levelProgress.bestScore = Math.Max(levelProgress.bestScore, request.score);
        int? nextLevelId = LevelService.GetNextLevelId(level, request.levelId);
        if (nextLevelId.HasValue) progression.highestUnlockedLevel = Math.Max(progression.highestUnlockedLevel, nextLevelId.Value);
        progression.levels[request.levelId] = levelProgress;
        return true;
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
