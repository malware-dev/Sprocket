
namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>Room, named for what it separates.</summary>
    public static class Spacing
    {
        /// <summary>A rule.</summary>
        public const float Hair = 1f;

        /// <summary>Between two lines that are one thing: a name, and what it is doing.</summary>
        public const float Line = 3f;

        /// <summary>Between one row of a list and the next.</summary>
        public const float Row = 6f;

        /// <summary>Between two controls sitting side by side.</summary>
        public const float Item = 10f;

        /// <summary>Between one region of a screen and another.</summary>
        public const float Region = 16f;

        /// <summary>Inside a row, across.</summary>
        public const float PadX = 19f;

        /// <summary>Inside a row, down.</summary>
        public const float PadY = 14f;

        /// <summary>Inside something small that is not a row: a chip, a switch, a quiet control.</summary>
        public const float ChipX = 16f;

        /// <summary>Inside something small that is not a row, down.</summary>
        public const float ChipY = 6f;

        /// <summary>Inside anything a value is given in, down: a box, a switch, a choice.</summary>
        public const float FieldY = ChipY * 1.3f;
    }
}