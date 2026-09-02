using DefaultNamespace;

namespace BloomKeeper.PlayFabFunctions.Models;

/// <summary>
/// Stores one winning level attempt's completion reward progress.
/// </summary>
public class CompletionRewardData
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;
    public string levelAttemptId;
    public int levelId;
    public int score;
    public int stars;
    public CompletionRewardStatus status;
    public DateTimeOffset? completedAtUtc;
    public List<CompletionRewardRecord> rewardRecords = new List<CompletionRewardRecord>();

    public CompletionRewardData()
    {
    }

    public CompletionRewardData(CompleteLevelAttemptRequest completeLevelAttemptRequest, IReadOnlyList<RewardRollResult> rewardRollResults)
    {
        // Validate the completion reward inputs.
        if (completeLevelAttemptRequest == null) throw new ArgumentNullException(nameof(completeLevelAttemptRequest));
        if (!completeLevelAttemptRequest.didWin) throw new ArgumentException("Completion rewards require a winning level attempt.", nameof(completeLevelAttemptRequest));
        if (rewardRollResults == null) throw new ArgumentNullException(nameof(rewardRollResults));
        if (rewardRollResults.Count != completeLevelAttemptRequest.stars) throw new ArgumentException("Completion reward roll count must match the earned stars.", nameof(rewardRollResults));

        // Snapshot the winning completion and its reward rolls.
        levelAttemptId = completeLevelAttemptRequest.attemptId;
        levelId = completeLevelAttemptRequest.levelId;
        score = completeLevelAttemptRequest.score;
        stars = completeLevelAttemptRequest.stars;
        status = CompletionRewardStatus.Prepared;
        foreach (RewardRollResult rewardRollResult in rewardRollResults)
        {
            rewardRecords.Add(new CompletionRewardRecord { reward = rewardRollResult.Reward, state = rewardRollResult.HasReward ? CompletionRewardState.Pending : CompletionRewardState.NoReward });
        }
    }

    /// <summary>
    /// [Duong] Throws if this saga does not match the completion request.
    /// </summary>
    public void ValidateCompletionRequest(CompleteLevelAttemptRequest completeLevelAttemptRequest)
    {
        if (completeLevelAttemptRequest == null) throw new ArgumentNullException(nameof(completeLevelAttemptRequest));
        if (levelAttemptId != completeLevelAttemptRequest.attemptId || levelId != completeLevelAttemptRequest.levelId || !completeLevelAttemptRequest.didWin || score != completeLevelAttemptRequest.score || stars != completeLevelAttemptRequest.stars) throw new InvalidOperationException("The stored completion reward no longer matches the level completion request.");
    }

    public bool RecordLevelCompletionCommitted()
    {
        // Keep an already recorded transition idempotent.
        if (status == CompletionRewardStatus.CompletionCommitted || status == CompletionRewardStatus.Completed) return false;

        // Require the prepared saga state.
        if (status != CompletionRewardStatus.Prepared) throw new InvalidOperationException("Only a prepared completion reward can record the level completion commit.");

        // Record the level completion commit.
        status = CompletionRewardStatus.CompletionCommitted;
        return true;
    }

    public bool RecordRewardApplied(int rewardIndex)
    {
        // Require a committed level completion.
        if (status != CompletionRewardStatus.CompletionCommitted) throw new InvalidOperationException("Completion rewards can only be applied after the level completion is committed.");

        // Keep an already recorded grant idempotent.
        CompletionRewardRecord completionRewardRecord = rewardRecords[rewardIndex];
        if (completionRewardRecord.state == CompletionRewardState.Applied) return false;

        // Require and record the pending grant.
        if (completionRewardRecord.state != CompletionRewardState.Pending) throw new InvalidOperationException($"Only a pending completion reward can be recorded as applied at index {rewardIndex}.");

        completionRewardRecord.state = CompletionRewardState.Applied;
        return true;
    }

    public bool RecordCompleted(DateTimeOffset completedAtUtc)
    {
        // Keep an already recorded completion idempotent.
        if (status == CompletionRewardStatus.Completed) return false;

        // Require every grant to be resolved.
        if (status != CompletionRewardStatus.CompletionCommitted) throw new InvalidOperationException("Only a committed completion reward can be completed.");
        if (rewardRecords.Any(completionRewardRecord => completionRewardRecord.state == CompletionRewardState.Pending)) throw new InvalidOperationException("Completion rewards cannot finish while a reward remains pending.");

        // Record the saga completion.
        status = CompletionRewardStatus.Completed;
        this.completedAtUtc = completedAtUtc;
        return true;
    }

    public static void ValidateCompletionRewardData(CompletionRewardData completionRewardData)
    {
        // Validate the saga identity and lifecycle state.
        if (completionRewardData == null) throw new InvalidOperationException("Completion reward data is missing.");
        if (completionRewardData.schemaVersion != CurrentSchemaVersion) throw new InvalidOperationException($"Completion reward data has unsupported schema version: {completionRewardData.schemaVersion}.");
        if (!Guid.TryParseExact(completionRewardData.levelAttemptId, "N", out _)) throw new InvalidOperationException("Completion reward data has an invalid level attempt ID.");
        if (completionRewardData.stars < 0) throw new InvalidOperationException("Completion reward data has negative stars.");
        if (completionRewardData.score < 0) throw new InvalidOperationException("Completion reward data has a negative score.");
        if (!Enum.IsDefined(typeof(CompletionRewardStatus), completionRewardData.status)) throw new InvalidOperationException($"Completion reward data has unsupported status: {completionRewardData.status}.");
        if (completionRewardData.rewardRecords == null || completionRewardData.rewardRecords.Count != completionRewardData.stars) throw new InvalidOperationException("Completion reward records must match the earned stars.");
        if (completionRewardData.status == CompletionRewardStatus.Completed && !completionRewardData.completedAtUtc.HasValue) throw new InvalidOperationException("Completed completion reward data has no completion time.");
        if (completionRewardData.status != CompletionRewardStatus.Completed && completionRewardData.completedAtUtc.HasValue) throw new InvalidOperationException("Unfinished completion reward data has a completion time.");

        // Validate every recorded reward roll.
        foreach (CompletionRewardRecord completionRewardRecord in completionRewardData.rewardRecords)
        {
            if (completionRewardRecord == null) throw new InvalidOperationException("Completion reward data contains a null reward record.");
            if (!Enum.IsDefined(typeof(CompletionRewardState), completionRewardRecord.state)) throw new InvalidOperationException($"Completion reward data contains unsupported reward state: {completionRewardRecord.state}.");
            if (completionRewardRecord.state == CompletionRewardState.NoReward && completionRewardRecord.reward != null) throw new InvalidOperationException("A no-reward completion roll contains a reward.");
            if (completionRewardRecord.state != CompletionRewardState.NoReward && completionRewardRecord.reward == null) throw new InvalidOperationException("A grantable completion reward record has no reward.");
            if (completionRewardData.status == CompletionRewardStatus.Prepared && completionRewardRecord.state == CompletionRewardState.Applied) throw new InvalidOperationException("A prepared completion reward contains an applied reward.");
            if (completionRewardData.status == CompletionRewardStatus.Completed && completionRewardRecord.state == CompletionRewardState.Pending) throw new InvalidOperationException("A completed completion reward contains a pending reward.");
        }
    }
}

public class CompletionRewardRecord
{
    public RewardGrant? reward;
    public CompletionRewardState state;
}

public enum CompletionRewardStatus
{
    Prepared = 1,
    CompletionCommitted = 2,
    Completed = 3
}

public enum CompletionRewardState
{
    NoReward = 1,
    Pending = 2,
    Applied = 3
}
