using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace Skills
{
    public interface ISkillRepresentationPresenter
    {
        Type RepresentationType { get; }
        UniTask Play(SkillRepresentationData representation, MatchGroupResolveResult resolution);
    }

    public abstract class SkillRepresentationPresenter<TRepresentation> : ISkillRepresentationPresenter where TRepresentation : SkillRepresentationData
    {
        public Type RepresentationType => typeof(TRepresentation);

        public UniTask Play(SkillRepresentationData representation, MatchGroupResolveResult resolution)
        {
            return Play((TRepresentation)representation, resolution);
        }

        protected abstract UniTask Play(TRepresentation representation, MatchGroupResolveResult resolution);
    }
}
