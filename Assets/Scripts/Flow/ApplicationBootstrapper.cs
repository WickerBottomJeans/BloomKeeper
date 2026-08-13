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

        private ApplicationStateMachine CreateApplicationStateMachine()
        {
            var guestCustomIdStore = new GuestCustomIdStore();
            var guestLoginService = new PlayFabGuestLoginService(guestCustomIdStore);
            var progressionService = new PlayFabProgressionService();
            var levelAttemptService = new PlayFabLevelAttemptService();
            var boosterInventoryService = new PlayFabBoosterInventoryService();
            var addressableContentService = new AddressableContentService();
            var bootFlow = new BootFlow(addressableContentService);
            var playerAccountLoader = new PlayerAccountLoader(progressionService, boosterInventoryService);
            var authFlow = new AuthFlow(guestLoginService, playerAccountLoader);
            var homeFlow = new HomeFlow(addressableContentService);
            var levelSetupFlow = new LevelSetupFlow(ConfigManager.Instance, levelAttemptService);
            var playLevelFlow = new PlayLevelFlow(levelSessionRuntime);
            var quitLevelFlow = new QuitLevelFlow(levelAttemptService);
            var finishLevelFlow = new FinishLevelFlow(ConfigManager.Instance, levelAttemptService);
            var settingsFlow = new SettingsFlow();
            return new ApplicationStateMachine(bootFlow, authFlow, homeFlow, levelSetupFlow, playLevelFlow, quitLevelFlow, finishLevelFlow, settingsFlow, musicStateController);
        }

        #endregion
    }
}
