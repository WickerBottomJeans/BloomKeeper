using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public interface ISkillAnimation
    {
        Type RepresentationType { get; }
        IEnumerable<Vector2Int> RequiredViews(SkillRepresentationData representation, BoardChangeGroup effect);
        UniTask Play(SkillRepresentationData representation, SkillPresentationContext skillPresentationContext);
    }

    public abstract class SkillAnimation<TRepresentation> : ISkillAnimation where TRepresentation : SkillRepresentationData
    {
        public Type RepresentationType => typeof(TRepresentation);

        public IEnumerable<Vector2Int> RequiredViews(SkillRepresentationData representation, BoardChangeGroup effect)
        {
            foreach (Vector2Int position in representation.ConsumedInputPositions) yield return position;
            foreach (Vector2Int position in AdditionalViews((TRepresentation)representation, effect)) yield return position;
        }

        public UniTask Play(SkillRepresentationData representation, SkillPresentationContext skillPresentationContext)
        {
            return Play((TRepresentation)representation, skillPresentationContext);
        }

        protected virtual IEnumerable<Vector2Int> AdditionalViews(TRepresentation representation, BoardChangeGroup effect)
        {
            yield break;
        }
        protected abstract UniTask Play(TRepresentation representation, SkillPresentationContext skillPresentationContext);
    }
}
