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
    public class SkillAnimationLibrary : MonoBehaviour
    {
        [SerializeField] private AudioCue prismaticBloomCue;
        [SerializeField] private AudioCue prismaticBloomFinishCue;

        private Dictionary<Type, ISkillAnimation> presenters;

        public void Init(PetalViewManager petalViewManager, BoardVFXManager boardVFXManager, BoardAudioManager boardAudioManager, BoardLayout layout)
        {
            presenters = new Dictionary<Type, ISkillAnimation>();
            Register(new BubbleSkillPresenter(petalViewManager, boardVFXManager, boardAudioManager, layout));
            Register(new StripedSkillPresenter(petalViewManager, boardVFXManager));
            Register(new StripeStripeFusionPresenter(petalViewManager, boardVFXManager));
            Register(new ButterflySkillPresenter(petalViewManager, boardVFXManager, layout));
            Register(new PrismaticBloomSkillPresenter(petalViewManager, boardVFXManager, layout, prismaticBloomCue, prismaticBloomFinishCue));
        }

        private void Register(ISkillAnimation presenter)
        {
            presenters.Add(presenter.RepresentationType, presenter);
        }

        public ISkillAnimation GetPresenter(SkillRepresentationData representation)
        {
            if (!presenters.TryGetValue(representation.GetType(), out ISkillAnimation presenter))
                throw new ArgumentOutOfRangeException(nameof(representation), representation.GetType(), "Skill representation is not supported.");
            return presenter;
        }
    }
}
