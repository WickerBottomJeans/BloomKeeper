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
        private bool isApplicationTransitionRunning;

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
            await PlayApplicationTransition(UIJawCurtainTipCategory.General, async () =>
            {
                authFlow.AuthCompleted -= HandleAuthCompleted;
                authFlow.AuthFailed -= HandleAuthFailed;
                authFlow.Exit();
                PlayerAccount account = await accountLoadFlow.Enter(authSession);
                accountLoadFlow.Exit();
                PlayerAccountContext.Instance.SetCurrentAccount(account);
                await EnterHome();
            });
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
            await PlayApplicationTransition(UIJawCurtainTipCategory.LevelStart, () =>
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
            ApplicationInputController.Instance.SetInputSuspended(true);
            try
            {
                await levelCompletionFlow.Enter(result);
                levelCompletionFlow.Exit();
                resultFlow.HomeRequested += HandleResultHomeRequested;
                resultFlow.RetryRequested += HandleResultRetryRequested;
                resultFlow.NextLevelRequested += HandleNextLevelRequested;
                resultFlow.Enter(result);
            }
            finally
            {
                ApplicationInputController.Instance.SetInputSuspended(false);
            }
        }

        private async void HandleResultHomeRequested()
        {
            await PlayApplicationTransition(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                ExitResultFlow();
                levelSessionFlow.LeaveLevel();
                await EnterHome();
            });
        }

        private async void HandleResultRetryRequested()
        {
            int levelId = currentResult.LevelId;
            await PlayApplicationTransition(UIJawCurtainTipCategory.Retry, () =>
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
            await PlayApplicationTransition(UIJawCurtainTipCategory.LevelStart, () =>
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

        private async UniTask PlayApplicationTransition(UIJawCurtainTipCategory tipCategory, Func<UniTask> whileClosedOperation, Action afterOpenedOperation = null)
        {
            if (isApplicationTransitionRunning)
                throw new InvalidOperationException("Cannot start an application transition while another transition is running.");

            isApplicationTransitionRunning = true;
            ApplicationInputController.Instance.SetInputSuspended(true);
            try
            {
                await UIManager.Instance.CloseJawCurtain(tipCategory);
                try
                {
                    await whileClosedOperation();
                }
                finally
                {
                    await UIManager.Instance.OpenJawCurtain();
                }

                afterOpenedOperation?.Invoke();
            }
            finally
            {
                ApplicationInputController.Instance.SetInputSuspended(false);
                isApplicationTransitionRunning = false;
            }
        }
    }
}
