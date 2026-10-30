
namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>Things that are a size rather than a gap.</summary>
    public static class Sizes
    {
        /// <summary>How far in a thing stands under what holds it: a slot under its category, an installation under its logic system, a nested step.</summary>
        public const float Indent = 32f;

        /// <summary>A block's picture.</summary>
        public const float Icon = 54f;

        /// <summary>How wide and tall a button in the strip between the library's panes is.</summary>
        public const float Move = 40f;

        /// <summary>How wide the strip between the library's panes is: a button with room either side.</summary>
        public const float MoveStrip = Move + Gaps.Within * 2f;

        /// <summary>One stage of a pick.</summary>
        public const float Column = 460f;

        /// <summary>The width of a dialog hanging over the screen.</summary>
        public const float Popup = 512f;

        /// <summary>How tall a codex tip grows at most.</summary>
        public const float TipHigh = 384f;

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

        /// <summary>How much larger than its mark an outline is drawn, so it lies round the mark: the font draws a mark three quarters as large in an outline's cell.</summary>
        public const float Halo = 1f / 0.75f;

        /// <summary>The width of a spine down a gutter: a blueprint's installations, an if's branches.</summary>
        public const float Spine = 2f;

        /// <summary>How far a spine turns in at its end, towards the last thing it runs beside.</summary>
        public const float SpineTurn = 12f;
    }
}