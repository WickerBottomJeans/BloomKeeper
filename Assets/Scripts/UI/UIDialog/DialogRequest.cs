namespace DefaultNamespace.UI
{
    public partial class DialogManager
    {
        private class DialogRequest
        {
            public DialogRequest(string title, string message, DialogOptionButton[] options, DialogSession session)
            {
                Title = title;
                Message = message;
                Options = options;
                Session = session;
            }

            public string Title { get; }
            public string Message { get; }
            public DialogOptionButton[] Options { get; }
            public DialogSession Session { get; }
        }
    }
}
