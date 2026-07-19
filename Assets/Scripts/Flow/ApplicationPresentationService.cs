using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public sealed class ApplicationPresentationService
    {
        public static ApplicationPresentationService Instance { get; } = new ApplicationPresentationService();

        private int activeBlockingOperationCount;
        private int activeLoadingOperationCount;
        private bool isCurtainTransitionRunning;

        private ApplicationPresentationService()
        {
        }

        public async UniTask<T> RunWithLoading<T>(Func<Task<T>> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            BeginLoadingOperation();
            try
            {
                return await operation();
            }
            finally
            {
                EndLoadingOperation();
            }
        }

        public async UniTask RunWithCurtain(UIJawCurtainTipCategory tipCategory, Func<UniTask> whileClosedOperation, Action afterOpenedOperation = null)
        {
            if (whileClosedOperation == null) throw new ArgumentNullException(nameof(whileClosedOperation));

            BeginCurtainTransition();
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
                EndCurtainTransition();
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

        private void BeginLoadingOperation()
        {
            BeginBlockingOperation();
            try
            {
                if (activeLoadingOperationCount == 0)
                    UIManager.Instance.ShowLoading();

                activeLoadingOperationCount++;
            }
            catch
            {
                EndBlockingOperation();
                throw;
            }
        }

        private void EndLoadingOperation()
        {
            if (activeLoadingOperationCount <= 0)
                throw new InvalidOperationException("Cannot end a loading presentation when none is running.");

            try
            {
                activeLoadingOperationCount--;
                if (activeLoadingOperationCount == 0)
                    UIManager.Instance.HideLoading();
            }
            finally
            {
                EndBlockingOperation();
            }
        }

        private void BeginCurtainTransition()
        {
            if (isCurtainTransitionRunning)
                throw new InvalidOperationException("Cannot start a curtain transition while another curtain transition is running.");

            BeginBlockingOperation();
            isCurtainTransitionRunning = true;
        }

        private void EndCurtainTransition()
        {
            try
            {
                EndBlockingOperation();
            }
            finally
            {
                isCurtainTransitionRunning = false;
            }
        }
    }
}
