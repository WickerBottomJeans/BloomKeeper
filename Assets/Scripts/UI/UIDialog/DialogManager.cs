using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.Utility;

namespace DefaultNamespace.UI
{
    public partial class DialogManager : Singleton<DialogManager>
    {
        private readonly Queue<DialogRequest> dialogQueue = new();
        private DialogRequest activeRequest;
        private UIManager uiManager;

        /// <summary>
        /// Queues the dialog and closes it after one button click.
        /// </summary>
        /// <returns>The clicked button type.</returns>
        public async UniTask<DialogButtonType> RunDialog(string title, string message, DialogOptionButton[] options, CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Length == 0) throw new ArgumentException("A dialog requires at least one option.", nameof(options));
            
            cancellationToken.ThrowIfCancellationRequested();
            DialogRequest request = EnqueueDialog(title, message, options);
            try
            {
                return await request.WaitForButtonClick().AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                CloseRequest(request);
            }
        }

        public async UniTask RunOkDialog(string title, string message, CancellationToken cancellationToken = default)
        {
            DialogOptionButton[] options = { DialogOptionButton.Ok };
            DialogButtonType buttonType = await RunDialog(title, message, options, cancellationToken);
            if (buttonType != DialogButtonType.Ok) throw new ArgumentOutOfRangeException(nameof(buttonType), buttonType, "Unsupported OK dialog button.");
        }

        public async UniTask<bool> RunRetryOrCancelDialog(string title, string message, CancellationToken cancellationToken = default)
        {
            DialogOptionButton[] options = { DialogOptionButton.Cancel, DialogOptionButton.Retry };
            DialogButtonType buttonType = await RunDialog(title, message, options, cancellationToken);
            switch (buttonType)
            {
                case DialogButtonType.Cancel:
                    return false;
                case DialogButtonType.Retry:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttonType), buttonType, "Unsupported Retry or Cancel dialog button.");
            }
        }

        private DialogRequest EnqueueDialog(string title, string message, params DialogOptionButton[] options)
        {
            DialogRequest request = new DialogRequest(title, message, options);
            dialogQueue.Enqueue(request);
            ProcessDialogQueue();
            return request;
        }

        private void ProcessDialogQueue()
        {
            // [Duong] Wait for the active dialog to finish.
            if (activeRequest != null) return;

            // [Duong] Find and present the next valid request.
            while (dialogQueue.Count > 0)
            {
                DialogRequest request = dialogQueue.Dequeue();

                // [Duong] Skip closed or failed requests.
                if (request.IsTerminal) continue;

                // [Duong] Present the request.
                activeRequest = request;
                try
                {
                    uiManager = UIManager.Instance;
                    uiManager.PresentDialogView(activeRequest.Title, activeRequest.Message, activeRequest.Options);
                    BindDialogEvents();
                    activeRequest.Activate();
                    return;
                }
                catch (Exception presentationException)
                {
                    // [Duong] Roll back the failed presentation.
                    Exception requestFailure = presentationException;
                    try
                    {
                        uiManager?.DismissDialogView();
                    }
                    catch (Exception dismissalException)
                    {
                        requestFailure = new AggregateException("Dialog presentation and dismissal both failed.", presentationException, dismissalException);
                    }

                    UnbindDialogEvents();
                    activeRequest = null;
                    request.Fail(requestFailure);
                }
            }
        }

        private void BindDialogEvents()
        {
            uiManager.DialogButtonClicked += HandleDialogButtonClicked;
        }

        private void UnbindDialogEvents()
        {
            if (uiManager == null) return;

            uiManager.DialogButtonClicked -= HandleDialogButtonClicked;
            uiManager = null;
        }

        private void HandleDialogButtonClicked(DialogButtonType buttonType)
        {
            uiManager.SetDialogButtonsInteractable(false);
            activeRequest.PublishButtonClick(buttonType);
        }

        private void CloseRequest(DialogRequest request)
        {
            if (request.IsTerminal) return;

            if (activeRequest != request)
            {
                request.CompleteClose();
                return;
            }

            activeRequest = null;
            try
            {
                uiManager.DismissDialogView();
            }
            finally
            {
                UnbindDialogEvents();
                request.CompleteClose();
                ProcessDialogQueue();
            }
        }

        private void OnDestroy()
        {
            UnbindDialogEvents();
            var exception = new ObjectDisposedException(nameof(DialogManager));

            activeRequest?.Fail(exception);
            activeRequest = null;

            while (dialogQueue.Count > 0)
                dialogQueue.Dequeue().Fail(exception);
        }

    }
}
