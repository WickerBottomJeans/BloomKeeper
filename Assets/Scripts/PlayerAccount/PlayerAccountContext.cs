using System;

namespace DefaultNamespace
{
    public class PlayerAccountContext
    {
        public PlayerAccount CurrentAccount { get; private set; }
        public bool HasAccount => CurrentAccount != null;

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
