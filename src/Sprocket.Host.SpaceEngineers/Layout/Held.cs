namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>
    /// What was being held down when something was pressed, said as what it means rather than as
    /// which key it was.
    /// </summary>
    /// <remarks>
    /// Named for the act because that is what travels: core is told which way a block was chosen and
    /// never which key did it, so remapping, a gamepad or a second gesture for the same act cost
    /// nothing. The keys themselves are read in one place, where the host reads every other input.
    /// </remarks>
    public enum Held
    {
        /// <summary>Nothing held. This one, and nothing else.</summary>
        Alone,

        /// <summary>This one as well, leaving the rest alone.</summary>
        Also,

        /// <summary>Everything from the last one taken to this one.</summary>
        Through
    }
}
