using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Controls the app's current state and flows.
    /// </summary>
    public class ApplicationStateMachine : IDisposable
    {
        private enum State
        {
            Booting,
            Auth,
            LoadingHome,
            Home,
            SettingUpLevel,
            PlayingLevel,
            QuittingLevel,
            PreparingLevelResult,
            LevelResult
        }

        private readonly BootFlow bootFlow;
        private readonly AuthFlow authFlow;
        private readonly HomeFlow homeFlow;
        private readonly LevelSetupFlow levelSetupFlow;
        private readonly PlayLevelFlow playLevelFlow;
        private readonly QuitLevelFlow quitLevelFlow;
        private readonly FinishLevelFlow finishLevelFlow;
        private readonly SettingsFlow settingsFlow;
        private readonly MusicStateController musicStateController;
        private State state;

        public ApplicationStateMachine(BootFlow bootFlow, AuthFlow authFlow, HomeFlow homeFlow, LevelSetupFlow levelSetupFlow, PlayLevelFlow playLevelFlow, QuitLevelFlow quitLevelFlow, FinishLevelFlow finishLevelFlow, SettingsFlow settingsFlow, MusicStateController musicStateController)
        {
            this.bootFlow = bootFlow ?? throw new ArgumentNullException(nameof(bootFlow));
            this.authFlow = authFlow ?? throw new ArgumentNullException(nameof(authFlow));
            this.homeFlow = homeFlow ?? throw new ArgumentNullException(nameof(homeFlow));
            this.levelSetupFlow = levelSetupFlow ?? throw new ArgumentNullException(nameof(levelSetupFlow));
            this.playLevelFlow = playLevelFlow ?? throw new ArgumentNullException(nameof(playLevelFlow));
            this.quitLevelFlow = quitLevelFlow ?? throw new ArgumentNullException(nameof(quitLevelFlow));
            this.finishLevelFlow = finishLevelFlow ?? throw new ArgumentNullException(nameof(finishLevelFlow));
            this.settingsFlow = settingsFlow ?? throw new ArgumentNullException(nameof(settingsFlow));
            this.musicStateController = musicStateController ?? throw new ArgumentNullException(nameof(musicStateController));
        }

        public async UniTask Start()
        {
            BindFlowEvents();
            await bootFlow.Run();
            state = State.Auth;
            authFlow.Enter();
        }

        public void Dispose()
        {
            authFlow.AccountReady -= HandleAccountReady;
            homeFlow.StartLevelRequested -= HandleHomeStartLevelRequested;
            homeFlow.SettingsRequested -= HandleSettingsRequested;
            playLevelFlow.LevelFinished -= HandleLevelFinished;
            playLevelFlow.QuitRequested -= HandleQuitRequested;
            playLevelFlow.SettingsRequested -= HandleSettingsRequested;
            finishLevelFlow.HomeRequested -= HandleResultHomeRequested;
            finishLevelFlow.RetryRequested -= HandleResultRetryRequested;
            finishLevelFlow.NextLevelRequested -= HandleResultNextLevelRequested;
        }

        private void BindFlowEvents()
        {
            authFlow.AccountReady += HandleAccountReady;
            homeFlow.StartLevelRequested += HandleHomeStartLevelRequested;
            homeFlow.SettingsRequested += HandleSettingsRequested;
            playLevelFlow.LevelFinished += HandleLevelFinished;
            playLevelFlow.QuitRequested += HandleQuitRequested;
            playLevelFlow.SettingsRequested += HandleSettingsRequested;
            finishLevelFlow.HomeRequested += HandleResultHomeRequested;
            finishLevelFlow.RetryRequested += HandleResultRetryRequested;
            finishLevelFlow.NextLevelRequested += HandleResultNextLevelRequested;
        }

        /// <summary>
        /// [Duong] When the account is readdy then we move on to home screen, start by loading it
        /// </summary>
        private void HandleAccountReady()
        {
            if (state == State.LoadingHome) return;
            if (state != State.Auth) throw new InvalidOperationException($"Cannot accept a ready account while the application is {state}.");

            state = State.LoadingHome;
            ApplicationOperationRunner.Instance.Run(EnterHomeFromAuth);
        }

        /// <summary>
        /// [Duong] Closes Auth behind the curtain, then enters Home.
        /// </summary>
        private async UniTask EnterHomeFromAuth()
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.General, async () =>
            {
                authFlow.Exit();
                await EnterHome();
            });
        }

        /// <summary>
        /// [Duong] Starts Home music, enters HomeFlow, and commits the Home state.
        /// </summary>
        private async UniTask EnterHome()
        {
            musicStateController.EnterHome();
            await homeFlow.Enter();
            state = State.Home;
        }

        /// <summary>
        /// Handles Home's level-start request and begins level setup.
        /// </summary>
        private void HandleHomeStartLevelRequested(int levelId)
        {
            if (state == State.SettingUpLevel) return;
            if (state != State.Home) throw new InvalidOperationException($"Cannot start a Home level while the application is {state}.");

            BeginLevelSetup(levelId, State.Home, UIJawCurtainTipCategory.LevelStart);
        }

        private void HandleResultRetryRequested(int levelId)
        {
            if (state == State.SettingUpLevel) return;
            if (state != State.LevelResult) throw new InvalidOperationException($"Cannot retry a level while the application is {state}.");

            BeginLevelSetup(levelId, State.LevelResult, UIJawCurtainTipCategory.Retry);
        }

        private void HandleResultNextLevelRequested(int levelId)
        {
            if (state == State.SettingUpLevel) return;
            if (state != State.LevelResult) throw new InvalidOperationException($"Cannot start the next level while the application is {state}.");

            BeginLevelSetup(levelId, State.LevelResult, UIJawCurtainTipCategory.LevelStart);
        }

        /// <summary>
        /// [Duong] Begin ... the level setup
        /// </summary>
        private void BeginLevelSetup(int levelId, State sourceState, UIJawCurtainTipCategory tipCategory)
        {
            state = State.SettingUpLevel;
            ApplicationOperationRunner.Instance.Run(() => TrySetupLevel(levelId, sourceState, tipCategory));
        }

        /// <summary>
        /// [Duong] Loads the level config and gets server approval, then exits the source flow and starts gameplay
        /// </summary>
        private async UniTask TrySetupLevel(int levelId, State sourceState, UIJawCurtainTipCategory tipCategory)
        {
            (LevelData levelData, string levelAttemptId)? setup = null;
            await ApplicationPresentationService.Instance.RunWithCurtain(tipCategory, async () =>
            {
                setup = await levelSetupFlow.TrySetup(levelId);
                if (!setup.HasValue)
                {
                    state = sourceState;
                    return;
                }

                ExitLevelSetupSource(sourceState, setup.Value.levelData.chapterId);
                playLevelFlow.Prepare(setup.Value.levelData, setup.Value.levelAttemptId);
                musicStateController.EnterLevel();
            }, () =>
            {
                if (!setup.HasValue) return;
                playLevelFlow.BeginGameplay();
                state = State.PlayingLevel;
            });
        }
        
        /// <summary>
        /// [Duong] Leaves the flow that requested playing this level in the first place
        /// </summary>
        private void ExitLevelSetupSource(State sourceState, int levelChapterId)
        {
            switch (sourceState)
            {
                case State.Home:
                    homeFlow.Exit();
                    return;
                case State.LevelResult:
                    finishLevelFlow.Exit();
                    homeFlow.SetCurrentChapter(levelChapterId);
                    return;
                default:
                    throw new InvalidOperationException($"Cannot commit level setup from {sourceState}.");
            }
        }

        private void HandleQuitRequested(string levelAttemptId)
        {
            if (state == State.QuittingLevel) return;
            if (state != State.PlayingLevel) throw new InvalidOperationException($"Cannot quit a level while the application is {state}.");

            state = State.QuittingLevel;
            ApplicationOperationRunner.Instance.Run(() => TryQuitLevel(levelAttemptId));
        }

        /// <summary>
        /// [Duong] Just ask server to abandon level attempt then go to home UI
        /// </summary>
        private async UniTask TryQuitLevel(string levelAttemptId)
        {
            if (!await quitLevelFlow.TryQuit(levelAttemptId))
            {
                state = State.PlayingLevel;
                return;
            }

            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                playLevelFlow.Exit();
                state = State.LoadingHome;
                await EnterHome();
            });
        }

        private void HandleLevelFinished(LevelSessionResult result, string levelAttemptId)
        {
            if (state != State.PlayingLevel) throw new InvalidOperationException($"Cannot prepare a level result while the application is {state}.");

            state = State.PreparingLevelResult;
            ApplicationOperationRunner.Instance.Run(() => PrepareLevelResult(result, levelAttemptId));
        }

        /// <summary>
        /// [Duong] Try submitting result to server and set up the level result screen
        /// </summary>
        private async UniTask PrepareLevelResult(LevelSessionResult result, string levelAttemptId)
        {
            await finishLevelFlow.CaptureBackground();
            playLevelFlow.Exit();
            if (await finishLevelFlow.TryEnter(result, levelAttemptId))
            {
                state = State.LevelResult;
                return;
            }

            finishLevelFlow.Exit();
            state = State.LoadingHome;
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.ReturnHome, EnterHome);
        }

        private void HandleResultHomeRequested()
        {
            if (state == State.LoadingHome) return;
            if (state != State.LevelResult) throw new InvalidOperationException($"Cannot leave a level result while the application is {state}.");

            state = State.LoadingHome;
            ApplicationOperationRunner.Instance.Run(ReturnHomeFromResult);
        }

        private async UniTask ReturnHomeFromResult()
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                finishLevelFlow.Exit();
                await EnterHome();
            });
        }

        private void HandleSettingsRequested()
        {
            if (state != State.Home && state != State.PlayingLevel) return;
            settingsFlow.Open();
        }
    }
}
