using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Boosters
{
    public sealed class BoosterRepresentationOrchestrator : MonoBehaviour
    {
        private Dictionary<Type, IBoosterRepresentationPresenter> presenters;

        public void Init(BoardVFXManager boardVFXManager)
        {
            presenters = new Dictionary<Type, IBoosterRepresentationPresenter>();
            Register(new BloomWandPresenter(boardVFXManager));
        }

        public void AcquireVitalViews(BoosterUseResult boosterUseResult, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            GetPresenter(boosterUseResult.Representation).AcquireAdditionalVitalViews(boosterUseResult.Representation, resolution, accessKeys);
        }

        public UniTask Play(BoosterUseResult boosterUseResult, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            return GetPresenter(boosterUseResult.Representation).Play(boosterUseResult.Representation, resolution, accessKeys).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        }

        private void Register(IBoosterRepresentationPresenter presenter)
        {
            presenters.Add(presenter.RepresentationType, presenter);
        }

        private IBoosterRepresentationPresenter GetPresenter(BoosterRepresentationData representation)
        {
            if (!presenters.TryGetValue(representation.GetType(), out IBoosterRepresentationPresenter presenter)) throw new ArgumentOutOfRangeException(nameof(representation), representation.GetType(), "Booster representation is not supported.");
            return presenter;
        }
    }
}
