namespace Reactor.Host.SpaceEngineers.Lists
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
        /// blinks a dot at exactly that rate, so "every ten ticks" is a thing you can see
        /// instead of a thing you have to picture. Whether to do that, and how, is its own
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
    }
}
