using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class ApplicationPresentationService
    {
        public static ApplicationPresentationService Instance { get; } = new ApplicationPresentationService();

        private int activeBlockingOperationCount;
        private int activeLoadingOperationCount;

        private ApplicationPresentationService()
        {
        }

        public async UniTask RunWithLoading(Func<UniTask> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            await BeginLoadingOperation();
            try
            {
                await operation();
            }
            finally
            {
                await EndLoadingOperation();
            }
        }

        public async UniTask<T> RunWithLoading<T>(Func<Task<T>> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            await BeginLoadingOperation();
            try
            {
                return await operation();
            }
            finally
            {
                await EndLoadingOperation();
            }
        }

        private void BeginBlockingOperation()
        {
            if (activeBlockingOperationCount == 0)
                ApplicationInputController.Instance.SetInputSuspended(true);

            activeBlockingOperationCount++;
        }

        private void EndBlockingOperation()
        {
            if (activeBlockingOperationCount <= 0)
                throw new InvalidOperationException("Cannot end a blocking presentation when none is running.");

            activeBlockingOperationCount--;
            if (activeBlockingOperationCount == 0)
                ApplicationInputController.Instance.SetInputSuspended(false);
        }

        private async UniTask BeginLoadingOperation()
        {
            BeginBlockingOperation();
            activeLoadingOperationCount++;
            try
            {
                await UIManager.Instance.ShowLoading();
            }
            catch
            {
                await EndLoadingOperation();
                throw;
            }
        }

        private async UniTask EndLoadingOperation()
        {
            if (activeLoadingOperationCount <= 0)
                throw new InvalidOperationException("Cannot end a loading presentation when none is running.");

            try
            {
                activeLoadingOperationCount--;
                if (activeLoadingOperationCount == 0)
                    await UIManager.Instance.HideLoading();
            }
            finally
            {
                EndBlockingOperation();
            }
        }

    }
}
