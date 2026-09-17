using System.Collections.Generic;
using System.ComponentModel;
using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Screen;

namespace Reactor.Host.SpaceEngineers.Sequences
{
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
            Steps = new ObservableList<SentenceStep>();
            AddOptions = new ObservableList<ChoiceEntry>();

            Steps.Changed += Listed;

            // Both lists, because the view reads both and is told rather than asking. The steps carry
            // things that announce for themselves and so are also listened to one by one; the adders
            // are plain choices, and their list changing shape is the whole of what can happen to them.
            AddOptions.Changed += Said;
            Trigger = trigger;
        }

        /// <summary>The steps this is repeating for, so each one can be let go of again.</summary>
        readonly List<SentenceStep> _heard = new List<SentenceStep>();

        /// <summary>
        /// Repeats what a step or the trigger says as something this sentence said.
        /// </summary>
        /// <remarks>
        /// A page writes a step's state every tick from what is running - which one is current, which
        /// one is broken - and each of those announces only when it actually moves. Gathering them here
        /// means a target listens to the sentence rather than to every line of it, and lets go of one
        /// thing rather than of a list it has to keep in step.
        /// <para>
        /// The steps are replaced whole whenever the sentence is rebuilt, so what to listen to is taken
        /// from the list each time it changes. The slots inside a step go with it.
        /// </para>
        /// </remarks>
        void Listed()
        {
            for (int i = 0; i < _heard.Count; i++)
                _heard[i].PropertyChanged -= Said;

            _heard.Clear();

            foreach (SentenceStep step in Steps)
            {
                step.PropertyChanged += Said;
                _heard.Add(step);
            }

            Raise(nameof(Steps));
        }

        void Said(object sender, PropertyChangedEventArgs e)
        {
            Raise(nameof(Steps));
        }

        void Said()
        {
            Raise(nameof(AddOptions));
        }

        public int SequenceId { get; private set; }

        /// <summary>
        /// Takes back the last change to this sequence. Zero when there is nothing to take
        /// back, or while it is running and nothing may be changed at all.
        /// </summary>
        /// <remarks>
        /// On the sentence rather than on the screen, because the history is per sequence -
        /// it is one of the things you do to this sequence, alongside pausing and deleting it,
        /// and it belongs with them.
        /// </remarks>
        public int UndoCommand
        {
            get { return _undoCommand; }
            set { Set(ref _undoCommand, value, nameof(UndoCommand)); }
        }

        int _undoCommand;

        /// <summary>
        /// What undoing would undo, e.g. "deleting a step".
        /// </summary>
        /// <remarks>
        /// "Undo" alone asks somebody to remember what they last did, which is exactly what a
        /// person who has just made a mistake is worst at.
        /// </remarks>
        public string UndoText
        {
            get { return _undoText; }
            set { Set(ref _undoText, value, nameof(UndoText)); }
        }

        string _undoText;

        /// <summary>
        /// Puts back the last thing undone. Zero when there is nothing waiting, or while it
        /// is running and nothing may be changed at all.
        /// </summary>
        public int RedoCommand
        {
            get { return _redoCommand; }
            set { Set(ref _redoCommand, value, nameof(RedoCommand)); }
        }

        int _redoCommand;

        /// <summary>What redoing would put back, e.g. "deleting a step".</summary>
        public string RedoText
        {
            get { return _redoText; }
            set { Set(ref _redoText, value, nameof(RedoText)); }
        }

        string _redoText;

        /// <summary>
        /// Whether this can be changed right now.
        /// </summary>
        /// <remarks>
        /// False while it is switched on, because a sequence that is switched on can be
        /// running, and rewiring a system mid-operation is how an airlock opens the outer
        /// door having never closed the inner one. Switching it off abandons any run in
        /// progress and leaves the system wherever it got to, which is exactly the state
        /// somebody needs in order to fix it.
        ///
        /// This is a fact about the system rather than a mode the editor is in. Nobody has to
        /// be taught it: you cannot rewire a live circuit either.
        ///
        /// The name is not covered. Renaming changes nothing about what the thing does.
        /// </remarks>
        public bool Editable
        {
            get { return _editable; }
            set { Set(ref _editable, value, nameof(Editable)); }
        }

        bool _editable;

        /// <summary>
        /// Why it cannot be changed, when it cannot.
        /// </summary>
        /// <remarks>
        /// Controls that quietly do nothing are a screen ignoring somebody. Whether this is
        /// shown as a banner, a tooltip or greyed rows is the target's business, but it has to
        /// be reachable somehow.
        /// </remarks>
        public string LockedText
        {
            get { return _lockedText; }
            set { Set(ref _lockedText, value, nameof(LockedText)); }
        }

        string _lockedText;

        public SentenceTrigger Trigger
        {
            get { return _trigger; }
            set
            {
                if (_trigger == value)
                    return;

                // Hooked and unhooked here, because this is the one place it changes. A page builds a
                // fresh trigger whenever the sentence is rebuilt, and the old one would otherwise keep
                // saying things to a sentence that has stopped showing it.
                if (_trigger != null)
                    _trigger.PropertyChanged -= Said;

                if (value != null)
                    value.PropertyChanged += Said;

                Set(ref _trigger, value, nameof(Trigger));
            }
        }

        SentenceTrigger _trigger;

        public ObservableList<SentenceStep> Steps { get; private set; }

        /// <summary>Ways to extend the sequence: "do something", "wait".</summary>
        public ObservableList<ChoiceEntry> AddOptions { get; private set; }

        /// <summary>Live: waiting, running, how long since it last ran.</summary>
        public string Status
        {
            get { return _status; }
            set { Set(ref _status, value, nameof(Status)); }
        }

        string _status;

        /// <summary>Lit briefly each time it fires.</summary>
        public bool JustRan
        {
            get { return _justRan; }
            set { Set(ref _justRan, value, nameof(JustRan)); }
        }

        bool _justRan;

        /// <summary>When it last ran, in words: "just now", "3 minutes ago", or empty.</summary>
        public string LastRun
        {
            get { return _lastRun; }
            set { Set(ref _lastRun, value, nameof(LastRun)); }
        }

        string _lastRun;

        /// <summary>
        /// How often the trigger fired while the sequence was already running and was
        /// therefore ignored. Surfaced rather than hidden, because a sequence that
        /// appears to ignore its own trigger is otherwise pure mystery.
        /// </summary>
        public int SkippedCount
        {
            get { return _skippedCount; }
            set { Set(ref _skippedCount, value, nameof(SkippedCount)); }
        }

        int _skippedCount;

        /// <summary>
        /// How often something asked for this sequence while it was switched off. Surfaced for the same
        /// reason: a rule that ignored a request is otherwise indistinguishable from one nothing asked.
        /// </summary>
        public int RefusedCount
        {
            get { return _refusedCount; }
            set { Set(ref _refusedCount, value, nameof(RefusedCount)); }
        }

        int _refusedCount;

        /// <summary>Set when the sequence refers to something no longer on the construct.</summary>
        public string Problem
        {
            get { return _problem; }
            set { Set(ref _problem, value, nameof(Problem)); }
        }

        string _problem;

        /// <summary>Whether <see cref="Problem" /> is a warning rather than something broken.</summary>
        public bool ProblemIsWarning
        {
            get { return _problemIsWarning; }
            set { Set(ref _problemIsWarning, value, nameof(ProblemIsWarning)); }
        }

        bool _problemIsWarning;

        /// <summary>
        /// Whether the sequence may fire. Switching a sequence off to see what changes is a
        /// normal way to work out what it was doing, so this is not deletion.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { Set(ref _enabled, value, nameof(Enabled)); }
        }

        bool _enabled;

        public int ToggleCommand
        {
            get { return _toggleCommand; }
            set { Set(ref _toggleCommand, value, nameof(ToggleCommand)); }
        }

        int _toggleCommand;

        public int DeleteCommand
        {
            get { return _deleteCommand; }
            set { Set(ref _deleteCommand, value, nameof(DeleteCommand)); }
        }

        int _deleteCommand;
    }
}
