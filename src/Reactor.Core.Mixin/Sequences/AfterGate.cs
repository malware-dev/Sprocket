using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// "after 30 ticks - …"
    /// </summary>
    /// <remarks>
    /// Still needed for things that genuinely have no completion signal, such as
    /// flashing a beacon. Offered second, never first.
    /// </remarks>
    public sealed class AfterGate : StepGate
    {
        public AfterGate(int ticks)
        {
            Ticks = ticks;
        }

        public int Ticks { get; set; }
    }
}
