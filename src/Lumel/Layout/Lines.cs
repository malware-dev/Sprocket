namespace Lumel.Layout
{
    /// <summary>How many lines a row of a <see cref="Form" /> takes: one while its label and field fit side by side, or always two.</summary>
    public enum Lines
    {
        /// <summary>Label and field side by side where the row has room for both, the field under the label where it has not.</summary>
        Fit,

        /// <summary>The field always under its label, across the whole width: text, and anything else that wants the room.</summary>
        Two
    }
}
