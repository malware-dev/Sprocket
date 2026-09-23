
namespace Sprocket.Host.SpaceEngineers.Picking
{
    /// <summary>One thing that can be picked.</summary>
    /// <remarks>
    /// Deliberately a superset rather than one type per kind of thing. Picking blocks uses the
    /// kind, the live status and the count; picking what happens to them uses none of those.
    /// One type means a target writes one of these once.
    /// </remarks>
    public sealed class PickOption
    {
        public PickOption(string text, int command)
        {
            Text = text;
            Command = command;
            Icon = "";
        }

        public string Text { get; set; }

        /// <summary>Something quieter about it, such as a block's kind.</summary>
        public string Secondary { get; set; }

        /// <summary>
        /// Names a picture for this option, or empty for none.
        /// </summary>
        /// <remarks>
        /// Passed straight through from the kind. Core neither draws it nor knows what it
        /// means; a target that has the artwork resolves the word, and one that has not
        /// ignores it and loses nothing but decoration.
        /// </remarks>
        public string Icon { get; set; }

        /// <summary>Live state, in the user's words. Empty for things that have none.</summary>
        public string Status { get; set; }

        /// <summary>Whether the thing is doing something right now.</summary>
        public bool IsActive { get; set; }

        public bool IsChosen { get; set; }

        public int Command { get; set; }

        /// <summary>
        /// Whether this names the group under it rather than being one of the things in it.
        /// </summary>
        /// <remarks>
        /// A row in the same list rather than a list of lists, because this list scrolls, narrows
        /// to a search and is matched against what is drawn by position. A heading kept outside it
        /// would sit still while the things it names scrolled past.
        /// </remarks>
        public bool IsHeading { get; set; }

        /// <summary>Whether a step doing this waits for it to finish.</summary>
        public bool Waits { get; set; }

        /// <summary>Whether this is listed to be read about rather than chosen.</summary>
        public bool IsWithheld { get; set; }

        /// <summary>What it says when the pointer rests on it, or null for nothing.</summary>
        public string Tip { get; set; }
    }
}
