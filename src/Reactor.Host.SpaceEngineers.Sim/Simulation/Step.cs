using VRageMath;

namespace Reactor.Sim.Simulation
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

        public int Kind;
        public Vector2 At;
        public Vector2 To;
        public int Count;
        public float Amount;
        public string Text;
    }
}
