using System;
using DefaultNamespace.UI;

namespace Boosters
{
    public sealed class BoosterUseResult
    {
        public BoardResolutionInput ResolutionInput { get; }
        public BoosterRepresentationData Representation { get; }

        public BoosterUseResult(BoardResolutionInput resolutionInput, BoosterRepresentationData representation)
        {
            ResolutionInput = resolutionInput ?? throw new ArgumentNullException(nameof(resolutionInput));
            Representation = representation ?? throw new ArgumentNullException(nameof(representation));
        }
    }
}
