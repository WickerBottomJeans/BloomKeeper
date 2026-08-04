using Cysharp.Threading.Tasks;
using DefaultNamespace.Audio;
using DefaultNamespace.UI;
using System;
using System.IO;
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
            var addressableContentService = new AddressableContentService();
            bootFlow = new BootFlow(addressableContentService);
            authFlow = new AuthFlow(guestLoginService);
            accountLoadFlow = new AccountLoadFlow(progressionService);
            levelCompletionFlow = new LevelCompletionFlow(progressionService);
            homeFlow = new HomeFlow(addressableContentService);
            levelSessionFlow = new LevelSessionFlow();
            settingsFlow = new SettingsFlow();
            resultFlow = new ResultFlow(ConfigManager.Instance);
            levelSessionFlow.SettingsRequested += HandleSettingsRequested;
        }

        private void EnterBootFlow()
        {
            bootFlow.BootCompleted += HandleBootCompleted;
            ApplicationOperationRunner.Instance.Run(bootFlow.Enter);
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
            ApplicationOperationRunner.Instance.Run(() => ProcessAuthCompletedAsync(authSession));
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
            ApplicationOperationRunner.Instance.Run(RunAuthFailureDialogAsync);
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
            homeFlow.SettingsRequested += HandleSettingsRequested;
            await homeFlow.Enter();
        }

        private void HandleStartLevelRequested(int levelId)
        {
            ApplicationOperationRunner.Instance.Run(() => StartLevelAsync(levelId));
        }

        private void HandleSettingsRequested()
        {
            settingsFlow.Enter();
        }

        private async UniTask StartLevelAsync(int levelId)
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.LevelStart, async () =>
            {
                homeFlow.StartLevelRequested -= HandleStartLevelRequested;
                homeFlow.SettingsRequested -= HandleSettingsRequested;
                homeFlow.Exit();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.QuitLevelRequested += HandleQuitLevelRequested;
                await levelSessionFlow.PrepareLevel(levelId);
                musicStateController.EnterGameplay();
            }, levelSessionFlow.StartPreparedLevel);
        }

        private void HandleQuitLevelRequested()
        {
            ApplicationOperationRunner.Instance.Run(RunQuitLevelDialogAsync);
        }

        private async UniTask RunQuitLevelDialogAsync()
        {
            var quitButton = new DialogOptionButton(DialogButtonType.Yes, "Quit", DialogButtonVariant.Orange);
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
            ApplicationOperationRunner.Instance.Run(() => ProcessLevelFinishedAsync(result));
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
            ApplicationOperationRunner.Instance.Run(ReturnHomeFromResultAsync);
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
            ApplicationOperationRunner.Instance.Run(RetryLevelFromResultAsync);
        }

        private async UniTask RetryLevelFromResultAsync()
        {
            int levelId = currentResult.LevelId;
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.Retry, async () =>
            {
                ExitResultFlow();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.QuitLevelRequested += HandleQuitLevelRequested;
                levelSessionFlow.LeaveLevel();
                await levelSessionFlow.PrepareLevel(levelId);
                musicStateController.EnterGameplay();
            }, levelSessionFlow.StartPreparedLevel);
        }

        private void HandleNextLevelRequested(int levelId)
        {
            ApplicationOperationRunner.Instance.Run(() => StartNextLevelFromResultAsync(levelId));
        }

        private async UniTask StartNextLevelFromResultAsync(int levelId)
        {
            LevelData nextLevelData;
            try
            {
                nextLevelData = await ApplicationPresentationService.Instance.RunWithLoading(() => ConfigManager.Instance.GetLevelDataAsync(levelId).AsTask());
            }
            catch (IOException exception)
            {
                Debug.LogError(exception);
                DialogOptionButton[] options = { DialogOptionButton.Ok };
                await DialogManager.Instance.RunDialogWorkflow("Next level unavailable", "This level is currently unavailable. Please try again later.", async session =>
                {
                    int buttonId = await session.WaitForButtonClick();
                    if ((DialogButtonType)buttonId != DialogButtonType.Ok)
                        throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported next-level unavailable dialog button.");
                }, options);
                return;
            }

            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.LevelStart, async () =>
            {
                ExitResultFlow();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.QuitLevelRequested += HandleQuitLevelRequested;
                levelSessionFlow.LeaveLevel();
                await levelSessionFlow.PrepareLevel(levelId);
                homeFlow.SetCurrentChapter(nextLevelData.chapterId);
                musicStateController.EnterGameplay();
            }, levelSessionFlow.StartPreparedLevel);
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
