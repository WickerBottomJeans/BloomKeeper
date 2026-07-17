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
        /// Queues the dialog, lets your workflow handle the clicks, then closes it when the workflow returns.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="workflow">Handles button clicks. Return from it to close the session.</param>
        /// <param name="options"></param>
        public async UniTask RunDialogWorkflow(string title, string message, Func<IDialogSession, UniTask> workflow, DialogOptionButton[] options, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DialogSession session = EnqueueDialog(title, message, cancellationToken, options);
            try
            {
                await workflow(session);
            }
            finally
            {
                CloseSession(session);
            }
        }

        private DialogSession EnqueueDialog(string title, string message, CancellationToken cancellationToken, params DialogOptionButton[] options)
        {
            DialogSession session = new DialogSession(HandleSessionButtonWaitStarted, cancellationToken);
            DialogRequest request = new DialogRequest(title, message, options, session);
            dialogQueue.Enqueue(request);
            ShowNextDialogIfIdle();
            return session;
        }

        private void ShowNextDialogIfIdle()
        {
            if (activeRequest != null) return;

            while (dialogQueue.Count > 0)
            {
                DialogRequest request = dialogQueue.Dequeue();
                if (request.Session.IsTerminal) continue;

                activeRequest = request;
                uiManager = UIManager.Instance;
                uiManager.PresentDialogView(activeRequest.Title, activeRequest.Message, activeRequest.Options);
                BindDialogEvents();
                activeRequest.Session.Activate();
                uiManager.SetDialogButtonsInteractable(activeRequest.Session.IsWaitingForButtonClick);
                return;
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

        private void HandleDialogButtonClicked(int buttonId)
        {
            uiManager.SetDialogButtonsInteractable(false);
            activeRequest.Session.PublishButtonClick(buttonId);
        }

        private void HandleSessionButtonWaitStarted(DialogSession session)
        {
            if (activeRequest == null || activeRequest.Session != session) return;

            uiManager.SetDialogButtonsInteractable(true);
        }

        private void CloseSession(DialogSession session)
        {
            if (session.IsTerminal) return;

            if (activeRequest == null || activeRequest.Session != session)
            {
                session.CompleteClose();
                return;
            }

            activeRequest = null;
            uiManager.DismissDialogView();
            UnbindDialogEvents();
            session.CompleteClose();
            ShowNextDialogIfIdle();
        }

        private void OnDestroy()
        {
            UnbindDialogEvents();
            var exception = new ObjectDisposedException(nameof(DialogManager));

            activeRequest?.Session.Fail(exception);
            activeRequest = null;

            while (dialogQueue.Count > 0)
                dialogQueue.Dequeue().Session.Fail(exception);
        }

    }
}
