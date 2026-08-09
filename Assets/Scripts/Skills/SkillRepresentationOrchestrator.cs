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
        [SerializeField] private AudioCue prismaticBloomCue;
        [SerializeField] private AudioCue prismaticBloomFinishCue;

        private Dictionary<Type, ISkillRepresentationPresenter> presenters;
        private PetalViewManager petalViewManager;

        public void Init(PetalViewManager petalViewManager, TileViewManager tileViewManager, BoardVFXManager boardVFXManager, BoardAudioManager boardAudioManager, BoardLayout layout)
        {
            this.petalViewManager = petalViewManager;
            presenters = new Dictionary<Type, ISkillRepresentationPresenter>();
            Register(new BubbleSkillPresenter(petalViewManager, tileViewManager, boardVFXManager, boardAudioManager, layout));
            Register(new StripedSkillPresenter(petalViewManager, tileViewManager, boardVFXManager));
            Register(new StripeStripeFusionPresenter(petalViewManager, tileViewManager, boardVFXManager));
            Register(new ButterflySkillPresenter(petalViewManager, tileViewManager, boardVFXManager, layout));
            Register(new PrismaticBloomSkillPresenter(petalViewManager, boardVFXManager, layout, prismaticBloomCue, prismaticBloomFinishCue));
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

        public UniTask Play(SkillUseResult skillResult, MatchGroupResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys, AudioPlaybackScope audioScope)
        {
            SkillRepresentationData representation = skillResult.Representation;
            if (representation == null)
                return UniTask.CompletedTask;
            if (audioScope == null)
                throw new ArgumentNullException(nameof(audioScope));

            return GetPresenter(representation).Play(representation, resolution, accessKeys, audioScope);
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
