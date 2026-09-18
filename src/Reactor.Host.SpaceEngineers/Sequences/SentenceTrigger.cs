using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Screen;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One trigger as the line it reads as: when it happens, and what it then does.
    /// </summary>
    /// <remarks>
    /// A sentence rather than a label, and made of the same slots a step is, because it is the same
    /// kind of thing to a person: words with the parts they chose sitting in them, each one pressable
    /// and each one opening the question it was answered with. A row that could only be pressed as a
    /// whole had to ask which half was meant, and the answer to that was already on the row.
    /// <para>
    /// Always a moment, never a situation. Trigger-action systems reliably trip people up by letting
    /// "when the door opens" and "while the door is open" look like the same sentence when they
    /// behave completely differently, so only the first kind exists.
    /// </para>
    /// </remarks>
    public sealed class SentenceTrigger : Observable
    {
        public SentenceTrigger(TriggerKind kind)
        {
            Kind = kind;
            Slots = new List<SentenceSlot>();
            Does = new List<SentenceSlot>();
            DoesLead = "";
        }

        /// <summary>Which sort of moment this is, which is what a target opens the line with.</summary>
        public TriggerKind Kind { get; private set; }

        /// <summary>What starts it: the blocks and the moment, or how often it comes round.</summary>
        public List<SentenceSlot> Slots { get; private set; }

        /// <summary>
        /// The word between the two halves: "runs", "stops", or nothing.
        /// </summary>
        /// <remarks>
        /// Nothing where the trigger does one plain thing, because then the second half is that act's
        /// own sentence - "close the outer door" - and a word in front of it would be a second verb.
        /// </remarks>
        public string DoesLead
        {
            get { return _doesLead; }
            set { Set(ref _doesLead, value, nameof(DoesLead)); }
        }

        string _doesLead;

        /// <summary>
        /// What it does: the sequence it is about, or the one act it performs itself.
        /// </summary>
        /// <remarks>
        /// The same slots a step uses for the plain case, so one thing done by a trigger and the same
        /// thing done by a step read alike and are edited alike.
        /// </remarks>
        public List<SentenceSlot> Does { get; private set; }

        /// <summary>
        /// Whether the watched signal is true at this instant.
        /// </summary>
        /// <remarks>
        /// Shown because a sequence that is idle while its signal reads true looks like a
        /// contradiction, and that apparent contradiction is exactly the event-versus-state
        /// distinction the user has to end up understanding.
        /// </remarks>
        public bool IsLiveNow
        {
            get { return _isLiveNow; }
            set { Set(ref _isLiveNow, value, nameof(IsLiveNow)); }
        }

        bool _isLiveNow;

        /// <summary>What is wrong with this trigger, in words, or empty.</summary>
        /// <remarks>
        /// A cleared button and a ground-off block are the trigger's own trouble, and this is the end
        /// of the trail that starts on the front door: the row says it, and pressing the row is the
        /// mend.
        /// </remarks>
        public string Problem
        {
            get { return _problem; }
            set { Set(ref _problem, value, nameof(Problem)); }
        }

        string _problem;

        public int DeleteCommand
        {
            get { return _deleteCommand; }
            set { Set(ref _deleteCommand, value, nameof(DeleteCommand)); }
        }

        int _deleteCommand;
    }
}
