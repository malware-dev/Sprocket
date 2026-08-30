using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// "once the inner door is shut - …"
    /// </summary>
    /// <remarks>
    /// The default kind of gate, and the important one. A duration is a programmer's
    /// substitute for a completion signal: people do not think "wait two seconds",
    /// they think "once the door is actually shut". Guessing a duration produces an
    /// airlock that works until the door is obstructed or the server lags, and then
    /// vents the ship.
    /// </remarks>
    public sealed class OnceGate : StepGate
    {
        public OnceGate(Condition condition)
        {
            Condition = condition;
        }

        public Condition Condition { get; set; }
    }
}
