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

        public SkillParticipant ParticipantA { get; }

        // Only set when two swapped petals trigger a combo together.
        public SkillParticipant? ParticipantB { get; }

        // The petal whose effect triggered this skill in a chain.
        public Petal TriggerPetal { get; }

        public SkillActivation(SpecialSkillType effectType, SkillParticipant participantA, SkillParticipant? participantB = null, Petal triggerPetal = null)
        {
            EffectType = effectType;
            ParticipantA = participantA;
            ParticipantB = participantB;
            TriggerPetal = triggerPetal;
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
        public MatchGroup MatchGroup { get; }
        public SkillRepresentationData Representation { get; }

        public SkillUseResult(
            MatchGroup matchGroup,
            SkillRepresentationData representation = null)
        {
            MatchGroup = matchGroup;
            Representation = representation;
        }
    }

}
