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
        private TileViewManager tileViewManager;

        public void Init(TileViewManager tileViewManager, BoardVFXManager boardVFXManager)
        {
            this.tileViewManager = tileViewManager;
            presenters = new Dictionary<Type, IBoosterRepresentationPresenter>();
            Register(new BloomWandPresenter(boardVFXManager));
        }

        public void ShowBoosterTargets(BoosterType boosterType, IReadOnlyList<Vector2Int> positions)
        {
            tileViewManager.ShowBoosterTargets(positions, boosterTargetPresentationConfig.GetMaterial(boosterType));
        }

        public void HideBoosterTargets()
        {
            tileViewManager.HideBoosterTargets();
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
