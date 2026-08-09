using System;
using System.Collections.Generic;

namespace Boosters
{
    public sealed class BoosterUseResult
    {
        public IReadOnlyList<MatchGroup> MatchGroups { get; }
        public BoosterRepresentationData Representation { get; }

        public BoosterUseResult(IReadOnlyList<MatchGroup> matchGroups, BoosterRepresentationData representation)
        {
            MatchGroups = matchGroups ?? throw new ArgumentNullException(nameof(matchGroups));
            Representation = representation ?? throw new ArgumentNullException(nameof(representation));
        }
    }
}
