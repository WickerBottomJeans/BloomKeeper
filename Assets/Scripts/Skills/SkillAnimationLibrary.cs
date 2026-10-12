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

        public void Init(PieceViewManager pieceViewManager, BoardVFXManager boardVFXManager, BoardAudioManager boardAudioManager, BoardLayout layout)
        {
            presenters = new Dictionary<Type, ISkillAnimation>();
            Register(new BubbleSkillPresenter(pieceViewManager, boardVFXManager, boardAudioManager, layout));
            Register(new StripedSkillPresenter(pieceViewManager, boardVFXManager));
            Register(new StripeStripeFusionPresenter(pieceViewManager, boardVFXManager));
            Register(new ButterflySkillPresenter(pieceViewManager, boardVFXManager, layout));
            Register(new PrismaticBloomSkillPresenter(pieceViewManager, boardVFXManager, layout, prismaticBloomCue, prismaticBloomFinishCue));
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
