namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>One option in a choice list.</summary>
    /// <remarks>
    /// Settable throughout, so a page that already has one can change what it says rather than
    /// making another. Rebuilding is what loses whatever somebody was doing with the thing.
    /// </remarks>
    public sealed class ChoiceEntry
    {
        public ChoiceEntry(string text, bool isCurrent, int command)
        {
            Text = text;
            IsCurrent = isCurrent;
            Command = command;
            Hint = "";
            Typed = "";
        }

        public string Text { get; set; }

        public bool IsCurrent { get; set; }

        public int Command { get; set; }

        /// <summary>
        /// The interval this option stands for, in ticks, or 0 if it stands for no interval.
        /// </summary>
        /// <remarks>
        /// Used by the repeat rates, where the label alone asks the reader to imagine a speed.
        /// Knowing the actual number lets a target show it rather than describe it - this host
        /// blinks a dot at exactly that rate, so "every ten ticks" is a thing to see
        /// instead of a thing to picture. Whether to do that, and how, is its own
        /// decision; core supplies the number.
        /// </remarks>
        public int IntervalTicks { get; set; }

        /// <summary>
        /// What picking this will cost, in plain words, or empty.
        /// </summary>
        /// <remarks>
        /// Shown beside the option rather than after choosing it. A warning that
        /// arrives once the damage is done is not a warning.
        /// </remarks>
        public string Warning { get; set; }

        /// <summary>
        /// Where a row that is typed into sends each keystroke, or 0 for a row that is pressed.
        /// </summary>
        /// <remarks>
        /// A typed row takes what is in it with <see cref="Command" />, which Enter in its box sends and
        /// the button beside the box sends too. Its <see cref="Text" /> is what that button says.
        /// </remarks>
        public int TextCommand { get; set; }

        /// <summary>What a typed row's box says while nothing is in it.</summary>
        public string Hint { get; set; }

        /// <summary>What a typed row holds, so a row made again says it again.</summary>
        public string Typed { get; set; }
    }
}
