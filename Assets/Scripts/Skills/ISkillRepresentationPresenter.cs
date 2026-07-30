using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace Skills
{
    public interface ISkillRepresentationPresenter
    {
        Type RepresentationType { get; }
        void AcquireAdditionalVitalViews(SkillRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
        UniTask Play(SkillRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
    }

    public abstract class SkillRepresentationPresenter<TRepresentation> : ISkillRepresentationPresenter where TRepresentation : SkillRepresentationData
    {
        public Type RepresentationType => typeof(TRepresentation);

        public void AcquireAdditionalVitalViews(SkillRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            AcquireAdditionalVitalViews((TRepresentation)representation, resolution, accessKeys);
        }

        public UniTask Play(SkillRepresentationData representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            return Play((TRepresentation)representation, resolution, accessKeys);
        }

        protected virtual void AcquireAdditionalVitalViews(TRepresentation representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
        }
        protected abstract UniTask Play(TRepresentation representation, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
    }
}
