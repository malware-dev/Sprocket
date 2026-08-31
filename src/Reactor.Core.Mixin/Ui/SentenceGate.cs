using System.Collections.Generic;

namespace Reactor.Ui
{
    /// <summary>
    /// What holds a step back, as it reads in front of the step.
    /// </summary>
    /// <remarks>
    /// Slots like any other part of the sentence, so that the day a gate becomes editable it is a
    /// command number appearing on one rather than a second way of saying the same thing. Until then
    /// they carry no command, which a target already reads as "not now".
    /// </remarks>
    public sealed class SentenceGate
    {
        public SentenceGate(GateKind kind)
        {
            Kind = kind;
            Slots = new List<SentenceSlot>();
        }

        public GateKind Kind { get; private set; }

        public List<SentenceSlot> Slots { get; private set; }
    }
}
