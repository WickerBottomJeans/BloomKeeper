namespace DefaultNamespace.UI
{
    public readonly struct DialogOptionButton
    {
        public static DialogOptionButton Ok => new DialogOptionButton(DialogButtonType.Ok, "OK", DialogButtonColorVariant.Green);
        public static DialogOptionButton Cancel => new DialogOptionButton(DialogButtonType.Cancel, "Cancel", DialogButtonColorVariant.Blue);
        public static DialogOptionButton Retry => new DialogOptionButton(DialogButtonType.Retry, "Retry", DialogButtonColorVariant.Green);

        public readonly DialogButtonType ButtonType;
        public readonly string Label;
        public readonly DialogButtonColorVariant ColorVariant;

        public DialogOptionButton(DialogButtonType buttonType, string label, DialogButtonColorVariant colorVariant)
        {
            ButtonType = buttonType;
            Label = label;
            ColorVariant = colorVariant;
        }
    }
}
