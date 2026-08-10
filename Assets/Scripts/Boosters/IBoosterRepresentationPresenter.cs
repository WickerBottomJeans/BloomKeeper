using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public interface IBoosterRepresentationPresenter
    {
        BoosterType BoosterType { get; }
        Type RepresentationType { get; }
        void ShowTargets(IReadOnlyList<Vector2Int> positions, BoosterTargetPresentationConfig.BoosterTargetMaterialMapping presentation);
        void SetTargetSelected(Vector2Int position, bool isSelected);
        void HideTargets();
        UniTask Play(BoosterRepresentationData representation);
    }

    public abstract class BoosterRepresentationPresenter<TRepresentation> : IBoosterRepresentationPresenter where TRepresentation : BoosterRepresentationData
    {
        public abstract BoosterType BoosterType { get; }
        public Type RepresentationType => typeof(TRepresentation);

        public abstract void ShowTargets(IReadOnlyList<Vector2Int> positions, BoosterTargetPresentationConfig.BoosterTargetMaterialMapping presentation);
        public abstract void SetTargetSelected(Vector2Int position, bool isSelected);
        public abstract void HideTargets();

        public UniTask Play(BoosterRepresentationData representation)
        {
            return Play((TRepresentation)representation);
        }

        protected abstract UniTask Play(TRepresentation representation);
    }
}
