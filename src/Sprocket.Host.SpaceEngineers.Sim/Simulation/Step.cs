using Sprocket.Host.SpaceEngineers.Layout;
using VRageMath;

namespace Sprocket.Sim.Simulation
{
    /// <summary>One thing the pointer or the clock does to the screen.</summary>
    public sealed class Step
    {
        public const int Press = 0;
        public const int Drag = 1;
        public const int Tick = 2;
        public const int Scroll = 3;
        public const int Type = 4;
        public const int Enter = 5;
        public const int Escape = 6;
        public const int Move = 7;
        public const int Clear = 8;

        /// <summary>Reads a program into the construct, as loading a save does.</summary>
        public const int Load = 9;

        /// <summary>Two presses in one gesture, which is not the same as two presses.</summary>
        public const int Double = 10;

        /// <summary>Onward to the next field of a form, which is Tab.</summary>
        public const int Onward = 11;

        /// <summary>An arrow key, which way held in <see cref="At" />: right and down are one, left and up minus one.</summary>
        public const int Arrow = 12;

        public int Kind;

        /// <summary>
        /// What was being held down for this step, which for a press says what it means.
        /// </summary>
        /// <remarks>
        /// Not a kind of its own, because a press with something held is still a press: it lands in
        /// the same place and reaches the same control, and only what that control makes of it
        /// differs.
        /// </remarks>
        public Held Held;
        public Vector2 At;
        public Vector2 To;
        public int Count;
        public float Amount;
        public string Text;
    }
}
