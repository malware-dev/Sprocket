using System.Collections.Generic;

namespace Reactor.Ui
{
    /// <summary>
    /// A fillable part of a sentence.
    /// </summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means. A target must render
    /// these as unmistakably fillable, and must not give anything else the same look.
    /// </remarks>
    public sealed class SentenceSlot
    {
        public SentenceSlot(string text, bool isEmpty, int command)
        {
            Text = text;
            IsEmpty = isEmpty;
            Command = command;
        }

        public string Text { get; set; }

        /// <summary>Nothing chosen yet, so it should read as an invitation.</summary>
        public bool IsEmpty { get; set; }

        public int Command { get; set; }
    }

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

        /// <summary>Offers the other kind of trigger, e.g. "use a repeat instead".</summary>
        public string SwitchLabel { get; set; }

        public int SwitchCommand { get; set; }
    }

    /// <summary>
    /// One numbered line of the sequence.
    /// </summary>
    /// <remarks>
    /// Flat and numbered. Nesting is what defeats novices, so there isn't any -
    /// every line sits at the same level and reads top to bottom.
    /// </remarks>
    public sealed class SentenceStep
    {
        public SentenceStep(int number)
        {
            Number = number;
            Slots = new List<SentenceSlot>();
        }

        public int Number { get; set; }

        /// <summary>A word before the slots, such as "wait". Often empty.</summary>
        public string Lead { get; set; }

        public List<SentenceSlot> Slots { get; private set; }

        /// <summary>
        /// The sequence is sitting on this step right now. Seeing execution move down the
        /// list is what makes a sequence readable instead of imaginable.
        /// </summary>
        public bool IsCurrent { get; set; }

        public int MoveUpCommand { get; set; }

        public int MoveDownCommand { get; set; }

        public int DeleteCommand { get; set; }
    }

    /// <summary>
    /// One sequence, written out as a sentence, with its live state.
    /// </summary>
    /// <remarks>
    /// The status here is not debugging instrumentation bolted on afterwards - it is
    /// the point. A user who can watch a sequence arm, fire, sit on step 3 and skip a
    /// re-trigger never has to run the program in their head, and running it in
    /// their head is the thing non-programmers actually cannot do.
    /// </remarks>
    public sealed class SequenceComponent : EditorComponent
    {
        public SequenceComponent(int sequenceId, SentenceTrigger trigger)
        {
            SequenceId = sequenceId;
            Trigger = trigger;
            Steps = new List<SentenceStep>();
            AddOptions = new List<ChoiceEntry>();
        }

        public int SequenceId { get; set; }

        /// <summary>
        /// What the user calls this, or empty.
        /// </summary>
        /// <remarks>
        /// Optional, and it has to look optional. A name buys two things - a list of
        /// twenty sequences becomes legible, and more than one trigger can start the
        /// same one - but requiring it would put a naming task in front of somebody
        /// who just wants a door to open. An unnamed sequence reads as its own
        /// sentence and is no worse off.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>Send the new name here when the user types.</summary>
        public int NameCommand { get; set; }

        /// <summary>
        /// Starts the sequence by hand, as if its trigger had fired.
        /// </summary>
        /// <remarks>
        /// Not a "run" button in the sense of a program being run - everything is
        /// always running. It is a stand-in for the thing that would normally set it
        /// off, so an airlock can be tested without walking to the button.
        /// </remarks>
        public int RunCommand { get; set; }

        public SentenceTrigger Trigger { get; set; }

        public List<SentenceStep> Steps { get; private set; }

        /// <summary>Ways to extend the sequence: "do something", "wait".</summary>
        public List<ChoiceEntry> AddOptions { get; private set; }

        /// <summary>Live: waiting, running, how long since it last ran.</summary>
        public string Status { get; set; }

        /// <summary>Lit briefly each time it fires.</summary>
        public bool Pulse { get; set; }

        /// <summary>
        /// How often the trigger fired while the sequence was already running and was
        /// therefore ignored. Surfaced rather than hidden, because a sequence that
        /// appears to ignore its own trigger is otherwise pure mystery.
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>Set when the sequence refers to something no longer on the construct.</summary>
        public string Problem { get; set; }

        /// <summary>
        /// Whether the sequence may fire. Switching a sequence off to see what changes is a
        /// normal way to work out what it was doing, so this is not deletion.
        /// </summary>
        public bool Enabled { get; set; }

        public int ToggleCommand { get; set; }

        public int DeleteCommand { get; set; }
    }
}
