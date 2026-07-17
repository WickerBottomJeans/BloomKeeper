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
            resultFlow = new ResultFlow();
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
            await UIManager.Instance.PlayJawCurtainTransition(UIJawCurtainTipCategory.General, async () =>
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
            await homeFlow.Enter(PlayerAccountContext.Instance.CurrentAccount.Progression);
        }

        private async void HandleStartLevelRequested(int levelId)
        {
            await UIManager.Instance.PlayJawCurtainTransition(UIJawCurtainTipCategory.LevelStart, () =>
            {
                homeFlow.StartLevelRequested -= HandleStartLevelRequested;
                homeFlow.Exit();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.StartLevel(levelId);
                return UniTask.CompletedTask;
            });
        }

        private async void HandleLevelFinished(LevelSessionResult result)
        {
            levelSessionFlow.LevelFinished -= HandleLevelFinished;
            currentResult = result;
            await levelCompletionFlow.Enter(result);
            levelCompletionFlow.Exit();
            resultFlow.HomeRequested += HandleResultHomeRequested;
            resultFlow.RetryRequested += HandleResultRetryRequested;
            resultFlow.Enter(result);
        }

        private async void HandleResultHomeRequested()
        {
            await UIManager.Instance.PlayJawCurtainTransition(UIJawCurtainTipCategory.ReturnHome, async () =>
            {
                ExitResultFlow();
                levelSessionFlow.LeaveLevel();
                await EnterHome();
            });
        }

        private async void HandleResultRetryRequested()
        {
            int levelId = currentResult.LevelId;
            await UIManager.Instance.PlayJawCurtainTransition(UIJawCurtainTipCategory.Retry, () =>
            {
                ExitResultFlow();
                levelSessionFlow.LevelFinished += HandleLevelFinished;
                levelSessionFlow.LeaveLevel();
                levelSessionFlow.StartLevel(levelId);
                return UniTask.CompletedTask;
            });
        }

        private void ExitResultFlow()
        {
            resultFlow.HomeRequested -= HandleResultHomeRequested;
            resultFlow.RetryRequested -= HandleResultRetryRequested;
            resultFlow.Exit();
            currentResult = null;
        }
    }
}
