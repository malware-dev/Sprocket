using System.Collections.Generic;

namespace Reactor.Ui
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
        public SentenceTrigger(string lead)
        {
            Lead = lead;
            Slots = new List<SentenceSlot>();
        }

        /// <summary>The opening word: "When" or "Every".</summary>
        public string Lead { get; set; }

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
    }
}
