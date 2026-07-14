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
        private HomeFlow homeFlow;
        private LevelSessionFlow levelSessionFlow;
        private ResultFlow resultFlow;
        private PlayerAccountContext playerAccountContext;
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
            homeFlow = new HomeFlow();
            levelSessionFlow = new LevelSessionFlow();
            resultFlow = new ResultFlow();
            playerAccountContext = new PlayerAccountContext();
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
                playerAccountContext.SetCurrentAccount(account);
                await EnterHome();
            });
        }

        private void HandleAuthFailed(Exception exception)
        {
            // TODO: just temporary, we will do a proper error notice later
            Debug.LogWarning(exception);
        }

        private async UniTask EnterHome()
        {
            homeFlow.StartLevelRequested += HandleStartLevelRequested;
            await homeFlow.Enter(playerAccountContext.CurrentAccount.Progression);
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

        private void HandleLevelFinished(LevelSessionResult result)
        {
            levelSessionFlow.LevelFinished -= HandleLevelFinished;
            currentResult = result;
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
