using System;
using DefaultNamespace;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Boosters
{
    public sealed class BoosterUseCoordinator : MonoBehaviour
    {
        public event Action<BoosterType> BoosterUseApproved;
        public event Action BoosterUseCanceled;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.pKey.wasPressedThisFrame)
            {
                BoosterUseApproved?.Invoke(BoosterType.BloomWand);
                return;
            }

            if (keyboard.cKey.wasPressedThisFrame)
                BoosterUseCanceled?.Invoke();
        }
#endif
    }
}
