using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public class SkillRepresentationOrchestrator : MonoBehaviour
    {
        [Header("Bomb Timing")]
        [SerializeField] private float bouquetDisappearDuration = 0.2f;

        [Header("Striped Timing")]
        [SerializeField] private float stripedPropagationDuration = 0.2f;

        [Header("Butterfly Timing")]
        [SerializeField] private float butterflyFlightDuration = 0.5f;
        [SerializeField] private float butterflyDisappearDuration = 0.2f;

        [Header("Stripe Sunburst Timing")]
        [SerializeField] private float stripeSunburstSpinDuration = 2f;
        [SerializeField] private float stripeSunburstMutationDuration = 1f;
        [SerializeField] private float stripeSunburstLaserDuration = 1f;
        [SerializeField] private float stripeSunburstLaserChargeUpDuration = 0.5f;

        private Dictionary<Type, ISkillRepresentationPresenter> presenters;

        public void Init(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardLayout layout, BoardCell[,] grid)
        {
            presenters = new Dictionary<Type, ISkillRepresentationPresenter>();
            Register(new BouquetSkillPresenter(petalViewManager, tileViewManager, boardVFXManager, grid, bouquetDisappearDuration));
            Register(new StripedSkillPresenter(petalViewManager, tileViewManager, boardVFXManager, grid, stripedPropagationDuration));
            Register(new ButterflySkillPresenter(petalViewManager, tileViewManager, layout, grid, butterflyFlightDuration, butterflyDisappearDuration));
            Register(new SunburstSkillPresenter(petalViewManager, tileViewManager, boardVFXManager, layout, grid, stripeSunburstSpinDuration, stripeSunburstMutationDuration, stripeSunburstLaserDuration, stripeSunburstLaserChargeUpDuration));
        }

        public UniTask Play(SkillUseResult skillResult, MatchGroupResolveResult resolution)
        {
            SkillRepresentationData representation = skillResult.Representation;
            if (representation == null)
                return UniTask.CompletedTask;

            if (!presenters.TryGetValue(representation.GetType(), out ISkillRepresentationPresenter presenter))
                throw new ArgumentOutOfRangeException(nameof(representation), representation.GetType(), "Skill representation is not supported.");

            return presenter.Play(representation, resolution);
        }

        private void Register(ISkillRepresentationPresenter presenter)
        {
            presenters.Add(presenter.RepresentationType, presenter);
        }
    }
}
