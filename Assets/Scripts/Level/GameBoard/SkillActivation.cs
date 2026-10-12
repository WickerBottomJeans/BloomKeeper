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
        public SkillExecutionType EffectType { get; }
        public IReadOnlyList<SkillParticipant> ConsumedInputs { get; }
        public SkillParticipant? SwapInitiator { get; }
        public SkillParticipant? SwapPartner { get; }

        // The color carried into a chained Prismatic Bloom effect.
        public PetalType? TriggerPetalType { get; }

        private SkillActivation(SkillExecutionType effectType, IReadOnlyList<SkillParticipant> consumedInputs, PetalType? triggerPetalType, SkillParticipant? swapInitiator, SkillParticipant? swapPartner)
        {
            if (consumedInputs.Count == 0)
                throw new ArgumentException("A skill activation requires at least one consumed input.", nameof(consumedInputs));

            EffectType = effectType;
            ConsumedInputs = new List<SkillParticipant>(consumedInputs).AsReadOnly();
            TriggerPetalType = triggerPetalType;
            SwapInitiator = swapInitiator;
            SwapPartner = swapPartner;
        }

        public static SkillActivation FromPetalSkill(SkillParticipant consumedInput, PetalType? triggerPetalType = null)
        {
            SkillExecutionType effectType = consumedInput.Petal.Skill switch
            {
                SpecialSkillType.StripedHorizontal => SkillExecutionType.StripedHorizontal,
                SpecialSkillType.StripedVertical => SkillExecutionType.StripedVertical,
                SpecialSkillType.Bubble => SkillExecutionType.Bubble,
                SpecialSkillType.PrismaticBloom => SkillExecutionType.PrismaticBloom,
                SpecialSkillType.Butterfly => SkillExecutionType.Butterfly,
                _ => throw new ArgumentOutOfRangeException(nameof(consumedInput), consumedInput.Petal.Skill, "Petal skill has no executable behavior.")
            };

            return new SkillActivation(effectType, new[] { consumedInput }, triggerPetalType, null, null);
        }

        public static SkillActivation FromSwap(SkillExecutionType effectType, SkillParticipant swapInitiator, SkillParticipant swapPartner)
        {
            return new SkillActivation(effectType, new[] { swapInitiator, swapPartner }, null, swapInitiator, swapPartner);
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
    /// [Duong] Describes a petal transformation to apply and present.
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

    /// <summary>
    /// [Duong] Stores what the skill intends to do (logic + visual data)
    /// </summary>
    public class SkillPlan
    {
        public IReadOnlyList<Vector2Int> AffectedPositions { get; }
        public IReadOnlyList<Vector2Int> ConsumedPositions { get; }
        public IReadOnlyList<PetalChange> PetalChanges { get; }
        public Petal Causer { get; }
        public PetalType? TriggerPetalType { get; }
        public SkillRepresentationData Representation { get; }

        public SkillPlan(IReadOnlyList<Vector2Int> affectedPositions, Petal causer, SkillRepresentationData representation, IReadOnlyList<Vector2Int> consumedPositions = null, IReadOnlyList<PetalChange> petalChanges = null, PetalType? triggerPetalType = null)
        {
            AffectedPositions = affectedPositions;
            ConsumedPositions = consumedPositions ?? Array.Empty<Vector2Int>();
            PetalChanges = petalChanges ?? Array.Empty<PetalChange>();
            Causer = causer;
            TriggerPetalType = triggerPetalType;
            Representation = representation;
        }
    }

}
