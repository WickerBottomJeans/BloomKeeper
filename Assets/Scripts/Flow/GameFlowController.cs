using Cysharp.Threading.Tasks;
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
        private ResultFlow resultFlow;
        private LevelSessionResult currentResult;

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
            resultFlow = new ResultFlow(new LevelCatalog(LevelLoader.LoadLevelMetas()));
        }

        private void EnterBootFlow()
        {
            bootFlow.BootCompleted += HandleBootCompleted;
            bootFlow.Enter();
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

        private async void HandleAuthCompleted(PlayFabAuthSession authSession)
        {
            if (!await TryLoadAccountAndEnterHome(authSession))
                RunAccountLoadFailureDialog(authSession).Forget();
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
            DialogOptionButton[] options = { DialogOptionButton.Cancel, DialogOptionButton.Retry };
            DialogManager.Instance.RunDialogWorkflow("Login failed", "Unable to connect. Check your connection and try again.", async session =>
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
            }, options).Forget();
        }

        private async UniTask EnterHome()
        {
            homeFlow.StartLevelRequested += HandleStartLevelRequested;
            await homeFlow.Enter();
        }

        private async void HandleStartLevelRequested(int levelId)
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.LevelStart, () =>
            {
                homeFlow.StartLevelRequested -= HandleStartLevelRequested;
                homeFlow.Exit();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.PrepareLevel(levelId);
                return UniTask.CompletedTask;
            }, levelSessionFlow.StartPreparedLevel);
        }

        private async void HandleLevelFinished(LevelSessionResult result)
        {
            levelSessionFlow.LevelFinished -= HandleLevelFinished;
            currentResult = result;
            if (!await TryCompleteLevelAndEnterResult())
                RunLevelCompletionFailureDialog().Forget();
        }

        private async UniTask<bool> TryCompleteLevelAndEnterResult()
        {
            try
            {
                await levelCompletionFlow.Enter(currentResult);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                return false;
            }

            levelCompletionFlow.Exit();
            resultFlow.HomeRequested += HandleResultHomeRequested;
            resultFlow.RetryRequested += HandleResultRetryRequested;
            resultFlow.NextLevelRequested += HandleNextLevelRequested;
            resultFlow.Enter(currentResult);
            return true;
        }

        private async UniTask RunLevelCompletionFailureDialog()
        {
            DialogOptionButton[] options = { DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow("Result submission failed", "Unable to save your level result. Check your connection and try again.", async session =>
            {
                while (true)
                {
                    int buttonId = await session.WaitForButtonClick();
                    switch ((DialogButtonType)buttonId)
                    {
                        case DialogButtonType.Retry:
                            if (await TryCompleteLevelAndEnterResult()) return;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported level completion failure dialog button.");
                    }
                }
            }, options);
        }

        private async void HandleResultHomeRequested()
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                ExitResultFlow();
                levelSessionFlow.LeaveLevel();
                await EnterHome();
            });
        }

        private async void HandleResultRetryRequested()
        {
            int levelId = currentResult.LevelId;
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.Retry, () =>
            {
                ExitResultFlow();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.LeaveLevel();
                levelSessionFlow.PrepareLevel(levelId);
                return UniTask.CompletedTask;
            }, levelSessionFlow.StartPreparedLevel);
        }

        private async void HandleNextLevelRequested(int levelId)
        {
            await ApplicationPresentationService.Instance.RunWithCurtain(UIJawCurtainTipCategory.LevelStart, () =>
            {
                ExitResultFlow();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.LeaveLevel();
                levelSessionFlow.PrepareLevel(levelId);
                return UniTask.CompletedTask;
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
