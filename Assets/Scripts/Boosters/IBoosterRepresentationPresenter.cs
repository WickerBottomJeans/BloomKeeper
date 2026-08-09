using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace Boosters
{
    public interface IBoosterRepresentationPresenter
    {
        Type RepresentationType { get; }
        void AcquireAdditionalVitalViews(BoosterRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
        UniTask Play(BoosterRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
    }

    public abstract class BoosterRepresentationPresenter<TRepresentation> : IBoosterRepresentationPresenter where TRepresentation : BoosterRepresentationData
    {
        public Type RepresentationType => typeof(TRepresentation);

        public void AcquireAdditionalVitalViews(BoosterRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            AcquireAdditionalVitalViews((TRepresentation)representation, resolution, accessKeys);
        }

        public UniTask Play(BoosterRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            return Play((TRepresentation)representation, resolution, accessKeys);
        }

        protected virtual void AcquireAdditionalVitalViews(TRepresentation representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
        }

        protected abstract UniTask Play(TRepresentation representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
    }
}
