using System.Collections.Generic;
using Skills;
using System;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public readonly struct SkillParticipant
    {
        public Vector2Int Position { get; }
        public Petal Petal { get; }

        public SkillParticipant(Vector2Int position, Petal petal)
        {
            Position = position;
            Petal = petal ?? throw new ArgumentNullException(nameof(petal));
        }
    }
    
    public readonly struct SkillActivation
    {
        public SpecialSkillType EffectType { get; }
        public IReadOnlyList<SkillParticipant> ConsumedInputs { get; }

        // The petal whose effect triggered this skill in a chain.
        public Petal TriggerPetal { get; }

        public SkillActivation(SpecialSkillType effectType, IReadOnlyList<SkillParticipant> consumedInputs, Petal triggerPetal = null)
        {
            if (consumedInputs.Count == 0)
                throw new ArgumentException("A skill activation requires at least one consumed input.", nameof(consumedInputs));

            EffectType = effectType;
            ConsumedInputs = new List<SkillParticipant>(consumedInputs).AsReadOnly();
            TriggerPetal = triggerPetal;
        }

        public SkillParticipant GetOnlyConsumedInput()
        {
            if (ConsumedInputs.Count != 1)
                throw new InvalidOperationException($"Skill activation requires exactly one consumed input but received {ConsumedInputs.Count}.");
            return ConsumedInputs[0];
        }

        public IReadOnlyList<Vector2Int> GetConsumedInputPositions()
        {
            var positions = new List<Vector2Int>(ConsumedInputs.Count);
            foreach (SkillParticipant input in ConsumedInputs)
                positions.Add(input.Position);
            return positions.AsReadOnly();
        }
    }
    
    /// <summary>
    /// Some skill executor directly edit Tile[,]
    /// </summary>
    public readonly struct PetalChange
    {
        public Vector2Int Position { get; }
        public Petal Before { get; }
        public Petal After { get; }

        public PetalChange(Vector2Int position, Petal before, Petal after)
        {
            Position = position;
            Before = before;
            After = after;
        }
    }

    public sealed class SkillUseResult
    {
        public MatchGroup InputMatchGroup { get; }
        public MatchGroup MatchGroup { get; }
        public SkillRepresentationData Representation { get; }

        public SkillUseResult(MatchGroup matchGroup, SkillRepresentationData representation = null, MatchGroup inputMatchGroup = null)
        {
            InputMatchGroup = inputMatchGroup;
            MatchGroup = matchGroup;
            Representation = representation;
        }

        public IEnumerable<MatchGroup> GetMatchGroups()
        {
            if (InputMatchGroup != null)
                yield return InputMatchGroup;
            yield return MatchGroup;
        }
    }

}
