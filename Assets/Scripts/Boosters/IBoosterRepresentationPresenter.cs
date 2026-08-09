using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using UnityEngine;

namespace Boosters
{
    public interface IBoosterRepresentationPresenter
    {
        BoosterType BoosterType { get; }
        Type RepresentationType { get; }
        void ShowTargets(IReadOnlyList<Vector2Int> positions, BoosterTargetPresentationConfig.BoosterTargetMaterialMapping presentation);
        void HideTargets();
        void AcquireAdditionalVitalViews(BoosterRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
        UniTask Play(BoosterRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys);
    }

    public abstract class BoosterRepresentationPresenter<TRepresentation> : IBoosterRepresentationPresenter where TRepresentation : BoosterRepresentationData
    {
        public abstract BoosterType BoosterType { get; }
        public Type RepresentationType => typeof(TRepresentation);

        public abstract void ShowTargets(IReadOnlyList<Vector2Int> positions, BoosterTargetPresentationConfig.BoosterTargetMaterialMapping presentation);
        public abstract void HideTargets();

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
