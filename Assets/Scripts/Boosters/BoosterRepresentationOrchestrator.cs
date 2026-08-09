using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Boosters
{
    public sealed class BoosterRepresentationOrchestrator : MonoBehaviour
    {
        [SerializeField] private BoosterTargetPresentationConfig boosterTargetPresentationConfig;

        private Dictionary<Type, IBoosterRepresentationPresenter> presenters;
        private Dictionary<BoosterType, IBoosterRepresentationPresenter> targetPresenters;
        private IBoosterRepresentationPresenter activeTargetPresenter;

        public void Init(TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardLayout boardLayout)
        {
            presenters = new Dictionary<Type, IBoosterRepresentationPresenter>();
            targetPresenters = new Dictionary<BoosterType, IBoosterRepresentationPresenter>();
            Register(new BloomWandPresenter(tileViewManager, boardVFXManager, boardLayout));
        }

        public void ShowBoosterTargets(BoosterType boosterType, IReadOnlyList<Vector2Int> positions)
        {
            if (activeTargetPresenter != null) throw new InvalidOperationException("Booster target presentation is already active.");

            IBoosterRepresentationPresenter presenter = GetPresenter(boosterType);
            presenter.ShowTargets(positions, boosterTargetPresentationConfig.GetPresentation(boosterType));
            activeTargetPresenter = presenter;
        }

        public void HideBoosterTargets()
        {
            if (activeTargetPresenter == null) throw new InvalidOperationException("Booster target presentation is not active.");

            activeTargetPresenter.HideTargets();
            activeTargetPresenter = null;
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
            targetPresenters.Add(presenter.BoosterType, presenter);
        }

        private IBoosterRepresentationPresenter GetPresenter(BoosterRepresentationData representation)
        {
            if (!presenters.TryGetValue(representation.GetType(), out IBoosterRepresentationPresenter presenter)) throw new ArgumentOutOfRangeException(nameof(representation), representation.GetType(), "Booster representation is not supported.");
            return presenter;
        }

        private IBoosterRepresentationPresenter GetPresenter(BoosterType boosterType)
        {
            if (!targetPresenters.TryGetValue(boosterType, out IBoosterRepresentationPresenter presenter)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster target presentation is not supported.");
            return presenter;
        }
    }
}
