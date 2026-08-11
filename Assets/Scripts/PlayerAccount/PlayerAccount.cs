namespace DefaultNamespace
{
    public class PlayerAccount
    {
        public PlayFabAuthSession AuthSession { get; }
        public PlayerProgressionData Progression { get; }
        public BoosterInventoryData BoosterInventory { get; private set; }

        public PlayerAccount(PlayFabAuthSession authSession, PlayerProgressionData progression, BoosterInventoryData boosterInventory)
        {
            AuthSession = authSession;
            Progression = progression;
            BoosterInventory = boosterInventory;
        }

        public void ReplaceBoosterInventory(BoosterInventoryData boosterInventory)
        {
            BoosterInventory = boosterInventory ?? throw new System.ArgumentNullException(nameof(boosterInventory));
        }
    }
}
