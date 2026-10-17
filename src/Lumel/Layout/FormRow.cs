namespace Lumel.Layout
{
    /// <summary>One row of a <see cref="Form" />: its label, or none for one element across the whole width, its field, and how many lines it may take.</summary>
    public sealed class FormRow
    {
        /// <summary>A row of <paramref name="label" />, or null, and <paramref name="field" />, on as many lines as <paramref name="lines" /> says.</summary>
        public FormRow(Element label, Element field, FormRowLines lines)
        {
            Label = label;
            Field = field;
            Lines = lines;
        }

        /// <summary>What the row is called, or null where its field stands alone.</summary>
        public Element Label { get; }

        /// <summary>Where the row's value is given.</summary>
        public Element Field { get; }

        /// <summary>Whether the row may sit on one line.</summary>
        public FormRowLines Lines { get; }

        /// <summary>Whether the last layout put the field under the label.</summary>
        public bool Stacked { get; set; }
    }
}
