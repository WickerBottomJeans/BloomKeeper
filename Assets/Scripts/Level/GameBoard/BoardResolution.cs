using System.Collections.Generic;

namespace DefaultNamespace.UI
{
    /// <summary>
    /// The result of one BoardLogic.Resolve call: matches, activated and chained skills,
    /// spawned skill petals, and their tile changes, all before gravity and refill.
    /// GameBoard.ResolveBoard may produce several of these results during its cascade loop
    /// before returning to Idle; this represents one resolution, not that entire loop.
    /// </summary>
    public class BoardResolution
    {
        public IReadOnlyList<BoardChangeGroup> Effects { get; }
        public IReadOnlyList<SkillPetalSpawn> SpawnedPetals { get; }
        public IReadOnlyList<TileChange> TileChanges { get; }
        public IReadOnlyList<ResolvedSkill> SkillResults { get; }

        public BoardResolution(List<BoardChangeGroup> effects, List<SkillPetalSpawn> spawnedPetals, IReadOnlyList<TileChange> tileChanges, IReadOnlyList<ResolvedSkill> skillResults)
        {
            Effects = new List<BoardChangeGroup>(effects).AsReadOnly();
            SpawnedPetals = new List<SkillPetalSpawn>(spawnedPetals).AsReadOnly();
            SkillResults = new List<ResolvedSkill>(skillResults).AsReadOnly();
            TileChanges = new List<TileChange>(tileChanges).AsReadOnly();
        }
    }

    /// <summary>
    /// [Duong] What actually happened when BoardLogic applied the SkillPlan
    /// </summary>
    public class ResolvedSkill
    {
        /// <summary>What the skill intended to change and how to show it.</summary>
        public SkillPlan skillPlan { get; }

        /// <summary>The tile changes caused by the skill.</summary>
        public BoardChangeGroup Effect { get; }

        /// <summary>The cleared skill inputs, or null if there were none.</summary>
        public BoardChangeGroup ConsumedInputs { get; }

        /// <summary>The tile change that activated this skill, or null for a direct swap.</summary>
        public TileChange Trigger { get; }

        public ResolvedSkill(SkillPlan skillPlan, BoardChangeGroup effect, BoardChangeGroup consumedInputs, TileChange trigger)
        {
            this.skillPlan = skillPlan;
            Effect = effect;
            ConsumedInputs = consumedInputs;
            Trigger = trigger;
        }
    }
}
