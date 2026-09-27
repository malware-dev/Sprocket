namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>How a picker was last narrowed: the kind of block, and what was typed into each search.</summary>
    /// <remarks>
    /// Somebody wiring a row of lights opens the picker once per step, and a filter that reset itself
    /// every time would be set again at every one of them.
    /// <para>
    /// One for choosing what starts a sequence and one for choosing what a step does, because they are
    /// different questions asked of different blocks - a button and a sensor start things, and lights
    /// and doors are what gets done to. See <see cref="Habits" />.
    /// </para>
    /// </remarks>
    public sealed class Filter
    {
        /// <summary>The kind the blocks were narrowed to, or empty for any.</summary>
        public string Kind = "";

        /// <summary>What was typed into the search over the blocks.</summary>
        public string Blocks = "";

        /// <summary>What was typed into the search over the kinds.</summary>
        public string Kinds = "";

        /// <summary>What was typed into the search over what can be done or waited for.</summary>
        public string Choices = "";
    }
}
