namespace DefaultNamespace.UI
{
    public readonly struct DialogOptionButton
    {
        public static DialogOptionButton Ok => new DialogOptionButton(DialogButtonType.Ok, "OK", UIButtonVariant.Green);
        public static DialogOptionButton Cancel => new DialogOptionButton(DialogButtonType.Cancel, "Cancel", UIButtonVariant.Blue);
        public static DialogOptionButton Yes => new DialogOptionButton(DialogButtonType.Yes, "Yes", UIButtonVariant.Green);
        public static DialogOptionButton No => new DialogOptionButton(DialogButtonType.No, "No", UIButtonVariant.Blue);
        public static DialogOptionButton Close => new DialogOptionButton(DialogButtonType.Close, "Close", UIButtonVariant.Blue);
        public static DialogOptionButton Retry => new DialogOptionButton(DialogButtonType.Retry, "Retry", UIButtonVariant.Green);

        public readonly int Id;
        public readonly string Label;
        public readonly UIButtonVariant Variant;

        public DialogOptionButton(int id, string label, UIButtonVariant variant)
        {
            Id = id;
            Label = label;
            Variant = variant;
        }

        public DialogOptionButton(DialogButtonType buttonType, string label, UIButtonVariant variant) : this((int)buttonType, label, variant)
        {
        }
    }
}
