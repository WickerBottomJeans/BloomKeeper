using System;

namespace Core
{
    public static class GlobalState
    {
        public static bool IsAdminMode { get; private set; }
        public static event Action<bool> OnAdminModeChanged;

        public static void SetAdminMode(bool active)
        {
            if (IsAdminMode == active) return;
            IsAdminMode = active;
            OnAdminModeChanged?.Invoke(active);
        }
    }
}