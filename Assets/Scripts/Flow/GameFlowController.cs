using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameFlowController : MonoBehaviour
    {
        private BootFlow bootFlow;
        private HomeFlow homeFlow;
        private LevelSessionFlow levelSessionFlow;
        private ResultFlow resultFlow;
        private LevelSessionResult currentResult;

        private void Awake()
        {
            InitializeFlows();
        }

        private async void Start()
        {
            await bootFlow.Enter();
            await EnterHome();
            await UIManager.Instance.OpenJawCurtain();
        }

        private void InitializeFlows()
        {
            bootFlow = new BootFlow();
            homeFlow = new HomeFlow();
            levelSessionFlow = new LevelSessionFlow();
            resultFlow = new ResultFlow();
        }

        private async UniTask EnterHome()
        {
            homeFlow.StartLevelRequested += HandleStartLevelRequested;
            await homeFlow.Enter();
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
