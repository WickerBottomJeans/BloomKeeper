using System;

namespace DefaultNamespace
{
    public class PlayerAccountContext
    {
        public static PlayerAccountContext Instance { get; } = new PlayerAccountContext();

        public PlayerAccount CurrentAccount { get; private set; }
        public bool HasAccount => CurrentAccount != null;

        private PlayerAccountContext()
        {
        }

        public void SetCurrentAccount(PlayerAccount account)
        {
            CurrentAccount = account ?? throw new ArgumentNullException(nameof(account));
        }

        public void Clear()
        {
            CurrentAccount = null;
        }
    }
}
