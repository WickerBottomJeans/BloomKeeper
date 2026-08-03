namespace DefaultNamespace.UI
{
    public readonly struct DialogOptionButton
    {
        public static DialogOptionButton Ok => new DialogOptionButton(DialogButtonType.Ok, "OK", DialogButtonVariant.Green);
        public static DialogOptionButton Cancel => new DialogOptionButton(DialogButtonType.Cancel, "Cancel", DialogButtonVariant.Blue);
        public static DialogOptionButton Yes => new DialogOptionButton(DialogButtonType.Yes, "Yes", DialogButtonVariant.Green);
        public static DialogOptionButton No => new DialogOptionButton(DialogButtonType.No, "No", DialogButtonVariant.Blue);
        public static DialogOptionButton Close => new DialogOptionButton(DialogButtonType.Close, "Close", DialogButtonVariant.Blue);
        public static DialogOptionButton Retry => new DialogOptionButton(DialogButtonType.Retry, "Retry", DialogButtonVariant.Green);

        public readonly int Id;
        public readonly string Label;
        public readonly DialogButtonVariant Variant;

        public DialogOptionButton(int id, string label, DialogButtonVariant variant)
        {
            Id = id;
            Label = label;
            Variant = variant;
        }

        public DialogOptionButton(DialogButtonType buttonType, string label, DialogButtonVariant variant) : this((int)buttonType, label, variant)
        {
        }
    }
}
