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
        }

        public string Text { get; set; }

        public bool IsCurrent { get; set; }

        public int Command { get; set; }
    }
}
