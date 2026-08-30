using System;
using Cysharp.Threading.Tasks;

namespace DefaultNamespace.UI
{
    public partial class DialogManager
    {
        private class DialogRequest
        {
            private readonly UniTaskCompletionSource<DialogButtonType> buttonClickCompletionSource = new UniTaskCompletionSource<DialogButtonType>();
            private DialogRequestState state = DialogRequestState.Queued;

            public DialogRequest(string title, string message, DialogOptionButton[] options)
            {
                Title = title;
                Message = message;
                Options = options;
            }

            public string Title { get; }
            public string Message { get; }
            public DialogOptionButton[] Options { get; }
            public bool IsTerminal => state == DialogRequestState.Closed || state == DialogRequestState.Faulted;

            public UniTask<DialogButtonType> WaitForButtonClick()
            {
                return buttonClickCompletionSource.Task;
            }

            public void Activate()
            {
                if (state != DialogRequestState.Queued)
                    throw new InvalidOperationException($"Cannot activate a dialog request in state: {state}.");

                state = DialogRequestState.Active;
            }

            public void PublishButtonClick(DialogButtonType buttonType)
            {
                if (state != DialogRequestState.Active)
                    throw new InvalidOperationException($"Cannot publish a button click to a dialog request in state: {state}.");
                if (!buttonClickCompletionSource.TrySetResult(buttonType))
                    throw new InvalidOperationException("Cannot publish more than one button click to a dialog request.");
            }

            public void CompleteClose()
            {
                if (IsTerminal)
                    throw new InvalidOperationException($"Cannot close a dialog request in state: {state}.");

                state = DialogRequestState.Closed;
            }

            public void Fail(Exception exception)
            {
                if (IsTerminal) return;

                state = DialogRequestState.Faulted;
                buttonClickCompletionSource.TrySetException(exception);
            }

            private enum DialogRequestState
            {
                Queued,
                Active,
                Closed,
                Faulted
            }
        }
    }
}
