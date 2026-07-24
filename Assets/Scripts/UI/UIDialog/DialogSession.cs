using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DefaultNamespace.UI
{
    public interface IDialogSession
    {
        CancellationToken CancellationToken { get; }
        UniTask<int> WaitForButtonClick();
    }

    public partial class DialogManager
    {
        private sealed class DialogSession : IDialogSession
        {
            private readonly Channel<int> buttonClicks = Channel.CreateSingleConsumerUnbounded<int>();
            private Action<DialogSession> buttonWaitStarted;
            private DialogSessionState state = DialogSessionState.Queued;
            private Exception failure;
            private bool isWaitingForButtonClick;

            public DialogSession(Action<DialogSession> buttonWaitStarted, CancellationToken cancellationToken)
            {
                this.buttonWaitStarted = buttonWaitStarted;
                CancellationToken = cancellationToken;
            }

            public CancellationToken CancellationToken { get; }
            public bool IsTerminal => state == DialogSessionState.Closed || state == DialogSessionState.Faulted;
            public bool IsWaitingForButtonClick => isWaitingForButtonClick;

            public async UniTask<int> WaitForButtonClick()
            {
                EnsureOpen();
                if (isWaitingForButtonClick)
                    throw new InvalidOperationException("A dialog session supports only one active button-click wait.");

                isWaitingForButtonClick = true;
                try
                {
                    buttonWaitStarted(this);
                    return await buttonClicks.Reader.ReadAsync(CancellationToken);
                }
                finally
                {
                    isWaitingForButtonClick = false;
                }
            }

            public void Activate()
            {
                if (state != DialogSessionState.Queued)
                    throw new InvalidOperationException($"Cannot activate a dialog session in state: {state}.");

                state = DialogSessionState.Active;
            }

            public void PublishButtonClick(int buttonId)
            {
                if (state != DialogSessionState.Active)
                    throw new InvalidOperationException($"Cannot publish a button click to a dialog session in state: {state}.");
                if (!isWaitingForButtonClick)
                    throw new InvalidOperationException("Cannot publish a button click when the dialog session is not waiting for one.");
                if (!buttonClicks.Writer.TryWrite(buttonId))
                    throw new InvalidOperationException("Cannot publish a button click to a closed dialog session.");
            }

            public void CompleteClose()
            {
                if (IsTerminal)
                    throw new InvalidOperationException($"Cannot close a dialog session in state: {state}.");

                state = DialogSessionState.Closed;
                buttonWaitStarted = null;
                buttonClicks.Writer.TryComplete();
            }

            public void Fail(Exception exception)
            {
                if (IsTerminal) return;

                state = DialogSessionState.Faulted;
                failure = exception;
                buttonWaitStarted = null;
                buttonClicks.Writer.TryComplete(exception);
            }

            private void EnsureOpen()
            {
                if (state == DialogSessionState.Closed)
                    throw new InvalidOperationException("The dialog session is already closed.");
                if (state == DialogSessionState.Faulted)
                    throw new InvalidOperationException("The dialog session has failed.", failure);
            }

            private enum DialogSessionState
            {
                Queued,
                Active,
                Closed,
                Faulted
            }
        }
    }
}
