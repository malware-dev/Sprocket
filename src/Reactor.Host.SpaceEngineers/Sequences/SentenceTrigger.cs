using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// The first line: what starts the sequence.
    /// </summary>
    /// <remarks>
    /// Always a moment, never a situation. Trigger-action systems reliably trip
    /// people up by letting "when the door opens" and "while the door is open" look
    /// like the same sentence when they behave completely differently, so only the
    /// first kind exists.
    /// </remarks>
    public sealed class SentenceTrigger
    {
        public SentenceTrigger(TriggerKind kind)
        {
            Kind = kind;
            Slots = new List<SentenceSlot>();
        }

        /// <summary>Which sort of moment this is, which is what a target opens the line with.</summary>
        public TriggerKind Kind { get; private set; }

        public List<SentenceSlot> Slots { get; private set; }

        /// <summary>
        /// Whether the watched signal is true at this instant.
        /// </summary>
        /// <remarks>
        /// Shown because a sequence that is idle while its signal reads true looks like a
        /// contradiction, and that apparent contradiction is exactly the
        /// event-versus-state distinction the user has to end up understanding.
        /// </remarks>
        public bool IsLiveNow { get; set; }

        /// <summary>
        /// Whether the reason this sequence cannot work is here, on this line.
        /// </summary>
        /// <remarks>
        /// What is wrong is said once, in words, by <see cref="SequenceComponent.Problem" />. This
        /// says where, and a target has to draw it: somebody led down two screens to get here came
        /// to be shown the thing to fix.
        /// </remarks>
        public bool HasProblem { get; set; }
    }
}
