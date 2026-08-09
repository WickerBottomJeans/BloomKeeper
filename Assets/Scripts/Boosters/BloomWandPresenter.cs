using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using DefaultNamespace.VFX;
using UnityEngine;

namespace Boosters
{
    public sealed class BloomWandPresenter : BoosterRepresentationPresenter<BloomWandRepresentationData>
    {
        private readonly BoardVFXManager boardVFXManager;

        public BloomWandPresenter(BoardVFXManager boardVFXManager)
        {
            this.boardVFXManager = boardVFXManager;
        }

        protected override UniTask Play(BloomWandRepresentationData representation, MatchResolveResult resolution, IDictionary<Vector2Int, ViewAccessKey> accessKeys)
        {
            return boardVFXManager.PlayBloomWandUntilImpact(representation.TargetPosition);
        }
    }
}
