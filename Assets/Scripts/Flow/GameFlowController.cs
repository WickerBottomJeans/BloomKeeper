using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameFlowController : MonoBehaviour
    {
        private BootFlow bootFlow;
        private AuthFlow authFlow;
        private AccountLoadFlow accountLoadFlow;
        private LevelCompletionFlow levelCompletionFlow;
        private HomeFlow homeFlow;
        private LevelSessionFlow levelSessionFlow;
        private SettingsFlow settingsFlow;
        private ResultFlow resultFlow;
        private LevelSessionResult currentResult;
        private bool isInFatalState;
        [SerializeField] private MusicStateController musicStateController;

        private void Awake()
        {
            InitializeFlows();
        }

        private void Start()
        {
            ApplicationInputController.Instance.SetUIInputActive(true);
            ApplicationInputController.Instance.SetGameBoardInputActive(false);
            EnterBootFlow();
        }

        private void InitializeFlows()
        {
            var guestCustomIdStore = new GuestCustomIdStore();
            var guestLoginService = new PlayFabGuestLoginService(guestCustomIdStore);
            var progressionService = new PlayFabProgressionService();
            bootFlow = new BootFlow();
            authFlow = new AuthFlow(guestLoginService);
            accountLoadFlow = new AccountLoadFlow(progressionService);
            levelCompletionFlow = new LevelCompletionFlow(progressionService);
            homeFlow = new HomeFlow();
            levelSessionFlow = new LevelSessionFlow();
            settingsFlow = new SettingsFlow();
            resultFlow = new ResultFlow(new LevelCatalog(LevelLoader.LoadLevelMetas()));
            levelSessionFlow.SettingsRequested += HandleSettingsRequested;
        }

        private void EnterBootFlow()
        {
            bootFlow.BootCompleted += HandleBootCompleted;
            RunFlowOperation(bootFlow.Enter);
        }

        private void HandleBootCompleted()
        {
            bootFlow.BootCompleted -= HandleBootCompleted;
            EnterAuthFlow();
        }

        private void EnterAuthFlow()
        {
            authFlow.AuthCompleted += HandleAuthCompleted;
            authFlow.AuthFailed += HandleAuthFailed;
            authFlow.Enter();
        }

        private void HandleAuthCompleted(PlayFabAuthSession authSession)
        {
            RunFlowOperation(() => ProcessAuthCompletedAsync(authSession));
        }

        private async UniTask ProcessAuthCompletedAsync(PlayFabAuthSession authSession)
        {
            if (!await TryLoadAccountAndEnterHome(authSession))
                await RunAccountLoadFailureDialog(authSession);
        }

        private async UniTask<bool> TryLoadAccountAndEnterHome(PlayFabAuthSession authSession)
        {
            bool accountLoaded = false;
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.General, async () =>
            {
                ExitAuthFlow();
                PlayerAccount account;
                try
                {
                    account = await accountLoadFlow.Enter(authSession);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(exception);
                    accountLoadFlow.Exit();
                    EnterAuthFlow();
                    return;
                }

                accountLoadFlow.Exit();
                PlayerAccountContext.Instance.SetCurrentAccount(account);
                await EnterHome();
                accountLoaded = true;
            });

            return accountLoaded;
        }

        private async UniTask RunAccountLoadFailureDialog(PlayFabAuthSession authSession)
        {
            DialogOptionButton[] options = { DialogOptionButton.Cancel, DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow("Account load failed", "Unable to load your account. Check your connection and try again.", async session =>
            {
                while (true)
                {
                    int buttonId = await session.WaitForButtonClick();
                    switch ((DialogButtonType)buttonId)
                    {
                        case DialogButtonType.Retry:
                            if (await TryLoadAccountAndEnterHome(authSession)) return;
                            break;
                        case DialogButtonType.Cancel:
                            return;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported account load failure dialog button.");
                    }
                }
            }, options);
        }

        private void ExitAuthFlow()
        {
            authFlow.AuthCompleted -= HandleAuthCompleted;
            authFlow.AuthFailed -= HandleAuthFailed;
            authFlow.Exit();
        }

        private void HandleAuthFailed(Exception exception)
        {
            Debug.LogWarning(exception);
            RunFlowOperation(RunAuthFailureDialogAsync);
        }

        private async UniTask RunAuthFailureDialogAsync()
        {
            DialogOptionButton[] options = { DialogOptionButton.Cancel, DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow("Login failed", "Unable to connect. Check your connection and try again.", async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                switch ((DialogButtonType)buttonId)
                {
                    case DialogButtonType.Retry:
                        authFlow.RetryLogin();
                        return;
                    case DialogButtonType.Cancel:
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported login failure dialog button.");
                }
            }, options);
        }

        private async UniTask EnterHome()
        {
            musicStateController.EnterHome();
            homeFlow.StartLevelRequested += HandleStartLevelRequested;
            await homeFlow.Enter();
        }

        private void HandleStartLevelRequested(int levelId)
        {
            RunFlowOperation(() => StartLevelAsync(levelId));
        }

        private void HandleSettingsRequested()
        {
            settingsFlow.Enter();
        }

        private async UniTask StartLevelAsync(int levelId)
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.LevelStart, () =>
            {
                homeFlow.StartLevelRequested -= HandleStartLevelRequested;
                homeFlow.Exit();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.QuitLevelRequested += HandleQuitLevelRequested;
                levelSessionFlow.PrepareLevel(levelId);
                musicStateController.EnterGameplay();
                return UniTask.CompletedTask;
            }, levelSessionFlow.StartPreparedLevel);
        }

        private void HandleQuitLevelRequested()
        {
            RunFlowOperation(RunQuitLevelDialogAsync);
        }

        private async UniTask RunQuitLevelDialogAsync()
        {
            var quitButton = new DialogOptionButton(DialogButtonType.Yes, "Quit", UIButtonVariant.Orange);
            DialogOptionButton[] options = { DialogOptionButton.Cancel, quitButton };
            bool quitConfirmed = false;
            await DialogManager.Instance.RunDialogWorkflow("Quit level?", "Your progress in this level will be lost.", async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                switch ((DialogButtonType)buttonId)
                {
                    case DialogButtonType.Cancel:
                        return;
                    case DialogButtonType.Yes:
                        quitConfirmed = true;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported quit-level dialog button.");
                }
            }, options);

            if (quitConfirmed)
                await QuitLevelAndEnterHome();
        }

        private async UniTask QuitLevelAndEnterHome()
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                levelSessionFlow.LevelFinished -= HandleLevelFinished;
                levelSessionFlow.QuitLevelRequested -= HandleQuitLevelRequested;
                levelSessionFlow.LeaveLevel();
                currentResult = null;
                await EnterHome();
            });
        }

        private void HandleLevelFinished(LevelSessionResult result)
        {
            RunFlowOperation(() => ProcessLevelFinishedAsync(result));
        }

        private async UniTask ProcessLevelFinishedAsync(LevelSessionResult result)
        {
            levelSessionFlow.LevelFinished -= HandleLevelFinished;
            levelSessionFlow.QuitLevelRequested -= HandleQuitLevelRequested;
            currentResult = result;
            await ResolveLevelCompletion();
        }

        private async UniTask ResolveLevelCompletion()
        {
            CompleteLevelAttemptResponse response;
            try
            {
                try
                {
                    response = await SubmitLevelCompletion();
                }
                catch (LevelCompletionSubmissionException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    response = await RunLevelCompletionRetryDialog();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                await RunLevelCompletionBackendFailureDialog();
                return;
            }

            if (response.outcome == CompleteLevelAttemptOutcome.Rejected)
            {
                await RunLevelCompletionRejectionDialog(response.rejectionReason.Value);
                return;
            }

            resultFlow.HomeRequested += HandleResultHomeRequested;
            resultFlow.RetryRequested += HandleResultRetryRequested;
            resultFlow.NextLevelRequested += HandleNextLevelRequested;
            resultFlow.Enter(currentResult);
        }

        private async UniTask<CompleteLevelAttemptResponse> SubmitLevelCompletion()
        {
            try
            {
                return await levelCompletionFlow.Enter(currentResult);
            }
            finally
            {
                levelCompletionFlow.Exit();
            }
        }

        private async UniTask<CompleteLevelAttemptResponse> RunLevelCompletionRetryDialog()
        {
            CompleteLevelAttemptResponse response = null;
            DialogOptionButton[] options = { DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow(LevelCompletionDialogText.RetryTitle, LevelCompletionDialogText.RetryMessage, async session =>
            {
                while (true)
                {
                    int buttonId = await session.WaitForButtonClick();
                    if ((DialogButtonType)buttonId != DialogButtonType.Retry)
                        throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported level completion failure dialog button.");

                    try
                    {
                        response = await SubmitLevelCompletion();
                        return;
                    }
                    catch (LevelCompletionSubmissionException exception) when (exception.IsRetryable)
                    {
                        Debug.LogWarning(exception);
                    }
                }
            }, options);

            if (response == null) throw new InvalidOperationException("Level completion retry dialog closed without a submission response.");
            return response;
        }

        private async UniTask RunLevelCompletionBackendFailureDialog()
        {
            DialogOptionButton[] options = { DialogOptionButton.Ok };
            await DialogManager.Instance.RunDialogWorkflow(LevelCompletionDialogText.BackendFailureTitle, LevelCompletionDialogText.BackendFailureMessage, async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                if ((DialogButtonType)buttonId != DialogButtonType.Ok)
                    throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported level completion backend failure dialog button.");
                await ReturnHomeFromCompletionHold();
            }, options);
        }

        private async UniTask RunLevelCompletionRejectionDialog(CompleteLevelAttemptRejectionReason rejectionReason)
        {
            DialogOptionButton[] options = { DialogOptionButton.Ok };
            await DialogManager.Instance.RunDialogWorkflow(LevelCompletionDialogText.RejectionTitle, LevelCompletionDialogText.GetRejectionMessage(rejectionReason), async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                if ((DialogButtonType)buttonId != DialogButtonType.Ok)
                    throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported level completion rejection dialog button.");
                await ReturnHomeFromCompletionHold();
            }, options);
        }

        private async UniTask ReturnHomeFromCompletionHold()
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                levelSessionFlow.LeaveLevel();
                currentResult = null;
                await EnterHome();
            });
        }

        private void HandleResultHomeRequested()
        {
            RunFlowOperation(ReturnHomeFromResultAsync);
        }

        private async UniTask ReturnHomeFromResultAsync()
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                ExitResultFlow();
                levelSessionFlow.LeaveLevel();
                await EnterHome();
            });
        }

        private void HandleResultRetryRequested()
        {
            RunFlowOperation(RetryLevelFromResultAsync);
        }

        private async UniTask RetryLevelFromResultAsync()
        {
            int levelId = currentResult.LevelId;
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.Retry, () =>
            {
                ExitResultFlow();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.QuitLevelRequested += HandleQuitLevelRequested;
                levelSessionFlow.LeaveLevel();
                levelSessionFlow.PrepareLevel(levelId);
                musicStateController.EnterGameplay();
                return UniTask.CompletedTask;
            }, levelSessionFlow.StartPreparedLevel);
        }

        private void HandleNextLevelRequested(int levelId)
        {
            RunFlowOperation(() => StartNextLevelFromResultAsync(levelId));
        }

        private async UniTask StartNextLevelFromResultAsync(int levelId)
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.LevelStart, () =>
            {
                ExitResultFlow();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.QuitLevelRequested += HandleQuitLevelRequested;
                levelSessionFlow.LeaveLevel();
                levelSessionFlow.PrepareLevel(levelId);
                musicStateController.EnterGameplay();
                return UniTask.CompletedTask;
            }, levelSessionFlow.StartPreparedLevel);
        }

        /// <summary>
        /// This one runs async work from a flow event
        /// U must handle expected failures inside the delegate or they’ll be treated as fatal.
        /// Must use on every flow event handler that starts async work.
        /// </summary>
        /// <param name="operation"></param>
        private void RunFlowOperation(Func<UniTask> operation)
        {
            if (isInFatalState) return;
            ObserveFlowOperationAsync(operation).Forget();
        }

        private async UniTask ObserveFlowOperationAsync(Func<UniTask> operation)
        {
            try
            {
                await operation();
            }
            catch (Exception exception)
            {
                try
                {
                    await EnterFatalStateAsync(exception);
                }
                catch (Exception fatalStateException)
                {
                    Debug.LogException(fatalStateException);
                    QuitApplication();
                }
            }
        }

        private async UniTask EnterFatalStateAsync(Exception exception)
        {
            if (isInFatalState) return;

            isInFatalState = true;
            Debug.LogException(exception);
            ApplicationInputController.Instance.SetGameBoardInputActive(false);

            DialogOptionButton[] options = { DialogOptionButton.Ok };
            await DialogManager.Instance.RunDialogWorkflow("Unexpected error", "BloomKeeper encountered an unexpected error and cannot continue safely. The game will close.", async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                if ((DialogButtonType)buttonId != DialogButtonType.Ok)
                    throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported fatal error dialog button.");
            }, options);

            QuitApplication();
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ExitResultFlow()
        {
            resultFlow.HomeRequested -= HandleResultHomeRequested;
            resultFlow.RetryRequested -= HandleResultRetryRequested;
            resultFlow.NextLevelRequested -= HandleNextLevelRequested;
            resultFlow.Exit();
            currentResult = null;
        }
    }
}
