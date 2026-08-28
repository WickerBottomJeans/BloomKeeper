using DefaultNamespace.Audio;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Creates the application's services and flows, then starts the state machine.
    /// </summary>
    public class ApplicationBootstrapper : MonoBehaviour
    {
        [SerializeField] private MusicStateController musicStateController;
        [SerializeField] private LevelSessionRuntime levelSessionRuntime;

        private ApplicationStateMachine applicationStateMachine;

        #region Unity Lifecycle

        private void Awake()
        {
            applicationStateMachine = CreateApplicationStateMachine();
        }

        private void Start()
        {
            ApplicationInputController.Instance.SetUIInputActive(true);
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            ApplicationOperationRunner.Instance.Run(applicationStateMachine.Start);
        }

        private void OnDestroy()
        {
            applicationStateMachine?.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the application services, flows, and state machine.
        /// </summary>
        private ApplicationStateMachine CreateApplicationStateMachine()
        {
            // Create shared services.
            var guestCustomIdStore = new GuestCustomIdStore();
            var guestLoginService = new PlayFabGuestLoginService(guestCustomIdStore);
            var playerStateService = new PlayFabPlayerStateService();
            var levelAttemptService = new PlayFabLevelAttemptService();
            var inventoryService = new PlayFabInventoryService();
            var playerLivesPresentationService = new PlayerLivesPresentationService();
            var addressableContentService = new AddressableContentService();

            // Create application flows.
            var bootFlow = new BootFlow(addressableContentService);
            var playerSessionLoader = new PlayerSessionLoader(playerStateService, inventoryService);
            var authFlow = new AuthFlow(guestLoginService, playerSessionLoader, playerLivesPresentationService);
            var homeFlow = new HomeFlow(addressableContentService, playerLivesPresentationService);
            var levelSetupFlow = new LevelSetupFlow(ConfigManager.Instance, levelAttemptService, playerLivesPresentationService);
            var playLevelFlow = new PlayLevelFlow(levelSessionRuntime);
            var quitLevelFlow = new QuitLevelFlow(levelAttemptService);
            var finishLevelFlow = new FinishLevelFlow(ConfigManager.Instance, levelAttemptService, inventoryService, playerLivesPresentationService);
            var settingsFlow = new SettingsFlow();
            return new ApplicationStateMachine(bootFlow, authFlow, homeFlow, levelSetupFlow, playLevelFlow, quitLevelFlow, finishLevelFlow, settingsFlow, musicStateController);
        }

        #endregion
    }
}
