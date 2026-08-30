using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// "only if the inner door is shut - …"
    /// </summary>
    /// <remarks>
    /// A guard, not a wait. If the situation does not hold the step is passed over
    /// and the sequence carries on - which is the difference between "hold here until
    /// it is safe" and "don't do this unless it is safe".
    ///
    /// Being passed over is recorded, because a step that quietly does nothing is
    /// indistinguishable from one that worked.
    /// </remarks>
    public sealed class OnlyIfGate : StepGate
    {
        public OnlyIfGate(Condition condition)
        {
            Condition = condition;
        }

        public Condition Condition { get; set; }
    }
}
