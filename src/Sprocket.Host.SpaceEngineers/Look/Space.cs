
namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>
    /// Room, named for what it separates.
    /// </summary>
    /// <remarks>
    /// Here for the same reason <see cref="Lettering" /> is: these were sixty literals scattered across five
    /// files, every one of them a judgement about how much air something needs, and none of them
    /// findable. Naming them makes a pass over the whole look six numbers rather than an archaeology
    /// expedition.
    /// <para>
    /// Sized for one of the game's own screens, which is four by three at the full height of the
    /// monitor - considerably more room than a window somebody sized with a mouse, and small numbers
    /// carried over from one would leave a third of this empty out of habit.
    /// </para>
    /// </remarks>
    public static class Space
    {
        /// <summary>A rule. One unit is one pixel at the height the UI is defined against.</summary>
        public const float Hair = 1f;

        /// <summary>Between two lines that are one thing - a name, and what it is doing.</summary>
        public const float Line = 3f;

        /// <summary>Between one row of a list and the next.</summary>
        public const float Row = 6f;

        /// <summary>Between two controls sitting side by side.</summary>
        public const float Item = 10f;

        /// <summary>Between one region of a screen and another.</summary>
        public const float Region = 16f;

        /// <summary>Inside a row, across.</summary>
        public const float PadX = 19f;

        /// <summary>Inside a row, down. Less than across, because a line of text is wider than it is tall.</summary>
        public const float PadY = 14f;

        /// <summary>Inside something small that is not a row - a chip, a switch, a quiet control.</summary>
        public const float ChipX = 16f;

        public const float ChipY = 6f;

        /// <summary>
        /// Inside anything a value is given in, down: a box, a switch, a choice. The one number that
        /// makes all of them one height when their words are one size.
        /// </summary>
        public const float FieldY = ChipY * 1.3f;
    }
}
