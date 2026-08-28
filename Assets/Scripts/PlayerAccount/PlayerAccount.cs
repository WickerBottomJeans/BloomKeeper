using System;

namespace DefaultNamespace
{
    /// <summary>
    /// Current logged-in player's data.
    /// </summary>
    public class PlayerAccount
    {
        public PlayFabAuthSession AuthSession { get; }
        public PlayerProgressionData Progression { get; private set; }
        public PlayerInventoryData PlayerInventory { get; private set; }

        public PlayerAccount(PlayFabAuthSession authSession, PlayerProgressionData progression, PlayerInventoryData playerInventory)
        {
            AuthSession = authSession;
            Progression = progression;
            PlayerInventory = playerInventory;
        }

        /// <summary>
        /// Replaces the player's current inventory.
        /// </summary>
        public void ReplacePlayerInventory(PlayerInventoryData playerInventory)
        {
            PlayerInventory = playerInventory ?? throw new ArgumentNullException(nameof(playerInventory));
        }

        /// <summary>
        /// Replaces the player's current progression.
        /// </summary>
        public void ReplacePlayerProgression(PlayerProgressionData progression)
        {
            Progression = progression ?? throw new ArgumentNullException(nameof(progression));
        }

        public void ApplyConfirmedLevelProgress(int levelId, LevelProgressData levelProgress, int highestUnlockedLevel)
        {
            Progression.highestUnlockedLevel = highestUnlockedLevel;
            Progression.levels[levelId] = levelProgress;
        }
    }
}
