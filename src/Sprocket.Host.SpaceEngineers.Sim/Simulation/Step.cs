using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using VRageMath;

namespace Sprocket.Sim.Simulation
{
    /// <summary>One thing the pointer or the clock does to the screen.</summary>
    public sealed class Step
    {
        /// <summary>A press at <see cref="At" />, with <see cref="Held" /> held down.</summary>
        public const int Press = 0;

        /// <summary>A press at <see cref="At" /> carried to <see cref="To" /> before it is let go.</summary>
        public const int Drag = 1;

        /// <summary><see cref="Count" /> ticks of the clock, drawn after each.</summary>
        public const int Tick = 2;

        /// <summary>The wheel turned by <see cref="Amount" /> over <see cref="At" />; positive scrolls down.</summary>
        public const int Scroll = 3;

        /// <summary><see cref="Text" /> typed into whatever holds the keyboard.</summary>
        public const int Type = 4;

        /// <summary>The Enter key.</summary>
        public const int Enter = 5;

        /// <summary>The Escape key.</summary>
        public const int Escape = 6;

        /// <summary>The pointer moved to <see cref="At" /> without pressing.</summary>
        public const int Move = 7;

        /// <summary>Everything typed in the box holding the keyboard taken away.</summary>
        public const int Clear = 8;

        /// <summary>The Alt key held from here on, which pins a tip.</summary>
        public const int Alt = 16;

        /// <summary>Reads a program into the construct, as loading a save does.</summary>
        public const int Load = 9;

        /// <summary>Two presses in one gesture, which is not the same as two presses.</summary>
        public const int Double = 10;

        /// <summary>Onward to the next field of a form, which is Tab.</summary>
        public const int Onward = 11;

        /// <summary>An arrow key, which way held in <see cref="At" />: right and down are one, left and up minus one.</summary>
        public const int Arrow = 12;

        /// <summary>A press on whatever says <see cref="Text" />, found where it is drawn at the moment it is pressed.</summary>
        public const int Named = 13;

        /// <summary>Hands the whole construct to the player <see cref="Count" /> names, then opens the editor again, as a capture and a look do.</summary>
        public const int Own = 14;

        /// <summary>A drag held where it has got to rather than let go, so the picture is of something being carried.</summary>
        public const int Carry = 15;

        /// <summary>Which of the kinds above it is.</summary>
        public int Kind;

        /// <summary>
        /// What was being held down for this step, which for a press says what it means.
        /// </summary>
        /// <remarks>
        /// Not a kind of its own, because a press with something held is still a press: it lands in
        /// the same place and reaches the same control, and only what that control makes of it
        /// differs.
        /// </remarks>
        public HeldModifier Held;
        /// <summary>Where it happens, in UI units; for an arrow, which way.</summary>
        public Vector2 At;

        /// <summary>Where a drag ends, in UI units.</summary>
        public Vector2 To;

        /// <summary>How many ticks a tick step runs.</summary>
        public int Count;

        /// <summary>How far a scroll turns the wheel.</summary>
        public float Amount;

        /// <summary>What is typed, the file a program is loaded from, or the words a named press finds.</summary>
        public string Text;
    }
}
