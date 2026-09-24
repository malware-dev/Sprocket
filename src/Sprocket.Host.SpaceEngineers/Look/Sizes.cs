namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>Things that are a size rather than a gap.</summary>
    public static class Sizes
    {
        /// <summary>A state light.</summary>
        public const float Dot = 13f;

        /// <summary>A block's picture.</summary>
        public const float Icon = 54f;

        /// <summary>One stage of a pick.</summary>
        public const float Column = 460f;

        /// <summary>The width of a dialog hanging over the screen.</summary>
        /// <remarks>Not for a menu, which is as wide as its words.</remarks>
        public const float Popup = 512f;

        /// <summary>The box at the head of a row in a list of several things to choose between.</summary>
        public const float Mark = 17f;

        /// <summary>How much room one character of a number takes, for sizing a box to what will go in it.</summary>
        public const float Digit = 16f;

        /// <summary>How narrow an editable name may get.</summary>
        /// <remarks>Zero means a nameplate falls back to its own height, a square.</remarks>
        public const float Name = 0f;

        /// <summary>A scrollbar, across.</summary>
        /// <remarks>The game reserves twelve units for its own.</remarks>
        public const float Bar = 12f;

        /// <summary>How short a scrollbar's thumb may get.</summary>
        public const float Thumb = 34f;
    }
}