using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameFlowController : MonoBehaviour
    {
        private HomeFlow homeFlow;
        private LevelSessionFlow levelSessionFlow;
        private ResultFlow resultFlow;
        private LevelSessionResult currentResult;

        private async void Start()
        {
            BootFlow bootFlow = new BootFlow();
            await bootFlow.Enter();
            EnterHome();
        }

        private void EnterHome()
        {
            homeFlow ??= new HomeFlow();
            homeFlow.StartLevelRequested += HandleStartLevelRequested;
            homeFlow.Enter();
        }

        private void HandleStartLevelRequested(int levelId)
        {
            homeFlow.StartLevelRequested -= HandleStartLevelRequested;
            homeFlow.Exit();
            levelSessionFlow ??= new LevelSessionFlow();
            levelSessionFlow.LevelFinished += HandleLevelFinished;
            levelSessionFlow.StartLevel(levelId);
        }

        private void HandleLevelFinished(LevelSessionResult result)
        {
            levelSessionFlow.LevelFinished -= HandleLevelFinished;
            currentResult = result;
            resultFlow ??= new ResultFlow();
            resultFlow.HomeRequested += HandleResultHomeRequested;
            resultFlow.RetryRequested += HandleResultRetryRequested;
            resultFlow.Enter(result);
        }

        private void HandleResultHomeRequested()
        {
            ExitResultFlow();
            levelSessionFlow.LeaveLevel();
            EnterHome();
        }

        private void HandleResultRetryRequested()
        {
            int levelId = currentResult.LevelId;
            ExitResultFlow();
            levelSessionFlow.LevelFinished += HandleLevelFinished;
            levelSessionFlow.LeaveLevel();
            levelSessionFlow.StartLevel(levelId);
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
