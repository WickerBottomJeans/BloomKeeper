using System;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class ApplicationOperationRunner
    {
        public static ApplicationOperationRunner Instance { get; } = new ApplicationOperationRunner();

        private bool isInFatalState;

        private ApplicationOperationRunner()
        {
        }

        /// <summary>
        /// Runs async work started by a synchronous event. Expected failures must be handled inside the operation; unhandled failures are fatal.
        /// </summary>
        public void Run(Func<UniTask> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (isInFatalState) return;
            ObserveAsync(operation).Forget();
        }

        private async UniTask ObserveAsync(Func<UniTask> operation)
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

        private  void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
