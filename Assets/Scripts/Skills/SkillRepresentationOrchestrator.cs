using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Skills
{
    public class SkillRepresentationOrchestrator : MonoBehaviour
    {
        [Header("Bubble Timing")]
        [SerializeField] private float bubblePrepareDuration = 0.4f;
        [SerializeField] private float bubbleFireDuration = 0.3f;
        [SerializeField] private float bubbleFinishDuration = 0.2f;

        [Header("Butterfly Timing")]
        [SerializeField] private float butterflyPrepareDuration = 0.1f;
        [SerializeField] private float butterflyFireDuration = 0.3f;
        [SerializeField] private float butterflyFinishDuration = 0.1f;

        [Header("Striped Timing")]
        [SerializeField] private float stripedPrepareDuration = 0.15f;
        [SerializeField] private float stripedFireDuration = 0.2f;
        [SerializeField] private float stripedFinishDuration = 0.1f;

        [Header("Prismatic Bloom Timing")]
        [SerializeField, Min(0f)] private float prismaticBloomPrepareDuration = 0.6f;
        [SerializeField, Min(0f)] private float prismaticBloomFireDuration = 1.2f;
        [SerializeField, Min(0f)] private float prismaticBloomMaximumSpinSpeed = 720f;

        private Dictionary<Type, ISkillRepresentationPresenter> presenters;
        private PetalViewManager petalViewManager;

        public void Init(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardAudioManager boardAudioManager, BoardLayout layout)
        {
            this.petalViewManager = petalViewManager;
            presenters = new Dictionary<Type, ISkillRepresentationPresenter>();
            Register(new BubbleSkillPresenter(petalViewManager, tileViewManager, boardVFXManager, boardAudioManager, layout, bubblePrepareDuration, bubbleFireDuration, bubbleFinishDuration));
            Register(new StripedSkillPresenter(petalViewManager, tileViewManager, boardVFXManager, stripedPrepareDuration, stripedFireDuration, stripedFinishDuration));
            Register(new StripeStripeFusionPresenter(petalViewManager, tileViewManager, boardVFXManager, stripedPrepareDuration, stripedFireDuration, stripedFinishDuration));
            Register(new ButterflySkillPresenter(petalViewManager, tileViewManager, boardVFXManager, layout, butterflyPrepareDuration, butterflyFireDuration, butterflyFinishDuration));
            Register(new PrismaticBloomSkillPresenter(petalViewManager, boardVFXManager, layout, prismaticBloomPrepareDuration, prismaticBloomFireDuration, prismaticBloomMaximumSpinSpeed));
        }

        public void AcquireVitalViews(SkillUseResult skillResult, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            SkillRepresentationData representation = skillResult.Representation;
            if (representation == null) return;
            foreach (Vector2Int position in representation.ConsumedInputPositions)
            {
                if (accessKeys.ContainsKey(position)) continue;
                if (!petalViewManager.TryAcquireView(position, nameof(SkillRepresentationOrchestrator), out ViewAccessKey accessKey))
                    throw new InvalidOperationException($"Consumed skill input view at {position} cannot be acquired by {nameof(SkillRepresentationOrchestrator)}.");
                accessKeys.Add(position, accessKey);
            }
            GetPresenter(representation).AcquireAdditionalVitalViews(representation, resolution, accessKeys);
        }

        public UniTask Play(SkillUseResult skillResult, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            SkillRepresentationData representation = skillResult.Representation;
            if (representation == null)
                return UniTask.CompletedTask;

            return GetPresenter(representation).Play(representation, resolution, accessKeys);
        }

        private void Register(ISkillRepresentationPresenter presenter)
        {
            presenters.Add(presenter.RepresentationType, presenter);
        }

        private ISkillRepresentationPresenter GetPresenter(SkillRepresentationData representation)
        {
            if (!presenters.TryGetValue(representation.GetType(), out ISkillRepresentationPresenter presenter))
                throw new ArgumentOutOfRangeException(nameof(representation), representation.GetType(), "Skill representation is not supported.");
            return presenter;
        }
    }
}
