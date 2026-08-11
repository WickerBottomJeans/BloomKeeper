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

        public PlayerProgressionData GetCurrentProgression()
        {
            if (CurrentAccount == null)
                throw new InvalidOperationException("Cannot access player progression before an account is loaded.");

            return CurrentAccount.Progression;
        }

        public BoosterInventoryData GetCurrentBoosterInventory()
        {
            if (CurrentAccount == null)
                throw new InvalidOperationException("Cannot access booster inventory before an account is loaded.");

            return CurrentAccount.BoosterInventory;
        }

        public void Clear()
        {
            CurrentAccount = null;
        }
    }
}
