using Lumel.Text;
using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>
    /// What the chrome around the page shows right now: the heading, the controls beside it, the way on at the foot, and
    /// any question standing over it all.
    /// </summary>
    /// <remarks>
    /// One screen at a time, with a back stack. That is a deliberate floor rather than a limitation: the editor has to
    /// work wherever it is embedded, and a layout assuming room for a sidebar plus a detail pane plus a sequence list is an
    /// assumption the smallest target cannot honor. The page draws its own view; this is only what is around it.
    /// <para>
    /// Written by the editor and read by the chrome, which asks <see cref="Revision" /> whether anything changed rather
    /// than reading everything on every frame.
    /// </para>
    /// </remarks>
    public sealed class EditorScreen
    {
        readonly List<ChoiceEntry> _actions = new List<ChoiceEntry>();
        readonly List<ChoiceEntry> _answers = new List<ChoiceEntry>();

        int _revision;
        string _subtitle = "";
        ICommand _library;
        ICommand _help;
        ICommand _flip;
        bool _switchIsOn;
        string _switchAbout;
        bool _switchLocked;
        bool _running;
        ICommand _runs;
        ICommand _stops;
        ICommand _deletes;
        string _deleteAbout = "";
        string _deleteLocked;
        bool _hasHistory;
        ICommand _undo;
        string _undoText = "";
        ICommand _redo;
        string _redoText = "";
        bool _canGoBack;
        ICommand _back;
        string _primaryText = "";
        string _primaryAbout = "";
        ICommand _primary;
        string _question = "";
        Arguments _questionArguments;
        bool _questionIsHere;
        ChoiceEntry _default;

        /// <summary>A screen showing nothing yet.</summary>
        public EditorScreen()
        {
            Heading = new List<HeadingPart>();
        }

        /// <summary>A number that differs from the last one read wherever anything here has changed since.</summary>
        /// <remarks>The heading's parts count too, since a page rewrites their words on a cadence.</remarks>
        public int Revision => _revision;

        /// <summary>What this screen is called, in order, one part at a time.</summary>
        /// <remarks>
        /// Never empty: every screen has at least one part, and most have exactly one. A sequence has two because it is two
        /// things - what starts it, and what it then does.
        /// <para>
        /// <b>There is deliberately no joined-up string of the whole thing here.</b> The whole reason this is a list is
        /// that a heading has to show which part of it is somebody's to change, and an API that offers the easy wrong thing
        /// gets the easy wrong thing.
        /// </para>
        /// </remarks>
        public List<HeadingPart> Heading { get; private set; }

        /// <summary>Context the title cannot carry - a count, a live status.</summary>
        public string Subtitle
        {
            get { return _subtitle; }
            set { Set(ref _subtitle, value ?? ""); }
        }

        /// <summary>Whether any part of the heading is somebody's to change.</summary>
        public bool CanRename
        {
            get
            {
                foreach (var part in Heading)
                {
                    if (part.CanRename)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Opens the blueprint library, or null on a screen that does not offer it.</summary>
        public ICommand Library
        {
            get { return _library; }
            set { Set(ref _library, value); }
        }

        /// <summary>Opens the codex at this screen's entry, or null on a screen that has none.</summary>
        public ICommand Help
        {
            get { return _help; }
            set { Set(ref _help, value); }
        }

        /// <summary>Switches this screen's subject the other way, or null when it has no such switch.</summary>
        public ICommand Flip
        {
            get { return _flip; }
            set { Set(ref _flip, value); }
        }

        /// <summary>Whether this screen has a switch at all.</summary>
        public bool HasSwitch => _flip != null;

        /// <summary>Whether the subject is currently allowed to run.</summary>
        public bool SwitchIsOn
        {
            get { return _switchIsOn; }
            set { Set(ref _switchIsOn, value); }
        }

        /// <summary>What pressing the switch would do, said as a note on it.</summary>
        public string SwitchAbout
        {
            get { return _switchAbout; }
            set { Set(ref _switchAbout, value); }
        }

        /// <summary>Whether the switch cannot be pressed now, which it shows dimmed with <see cref="SwitchAbout" /> saying why.</summary>
        public bool SwitchLocked
        {
            get { return _switchLocked; }
            set { Set(ref _switchLocked, value); }
        }

        /// <summary>Whether the subject is running now, which turns the run mark into a stop.</summary>
        public bool Running
        {
            get { return _running; }
            set { Set(ref _running, value); }
        }

        /// <summary>Runs the subject by hand, or null where it cannot be run from here.</summary>
        public ICommand Runs
        {
            get { return _runs; }
            set { Set(ref _runs, value); }
        }

        /// <summary>Stops the subject, or null where it cannot be stopped from here.</summary>
        public ICommand Stops
        {
            get { return _stops; }
            set { Set(ref _stops, value); }
        }

        /// <summary>Deletes the subject, or null where it cannot be deleted from here.</summary>
        public ICommand Deletes
        {
            get { return _deletes; }
            set { Set(ref _deletes, value); }
        }

        /// <summary>What deleting the subject does, said as the bin's note.</summary>
        public string DeleteAbout
        {
            get { return _deleteAbout; }
            set { Set(ref _deleteAbout, value ?? ""); }
        }

        /// <summary>Why the subject cannot be deleted now, which the bin says dimmed in its place; null where nothing holds it.</summary>
        public string DeleteLocked
        {
            get { return _deleteLocked; }
            set { Set(ref _deleteLocked, value); }
        }

        /// <summary>Whether this screen has a way back from what was done on it.</summary>
        /// <remarks>
        /// Separate from having anything to walk back right now, because the pair holds its place either way: arrows that
        /// appeared with the first edit would shift the switch beside them out from under the pointer that had just made
        /// it.
        /// </remarks>
        public bool HasHistory
        {
            get { return _hasHistory; }
            set { Set(ref _hasHistory, value); }
        }

        /// <summary>Takes back the last change made on this screen, or null when there is nothing to take back.</summary>
        public ICommand Undo
        {
            get { return _undo; }
            set { Set(ref _undo, value); }
        }

        /// <summary>What undoing would undo, e.g. "deleting a trigger".</summary>
        /// <remarks>
        /// "Undo" alone asks somebody to remember what they last did, which is exactly what a person who has just made a
        /// mistake is worst at.
        /// </remarks>
        public string UndoText
        {
            get { return _undoText; }
            set { Set(ref _undoText, value ?? ""); }
        }

        /// <summary>Puts back the last thing undone, or null when there is nothing waiting.</summary>
        public ICommand Redo
        {
            get { return _redo; }
            set { Set(ref _redo, value); }
        }

        /// <summary>What redoing would put back, in the words it happened in.</summary>
        public string RedoText
        {
            get { return _redoText; }
            set { Set(ref _redoText, value ?? ""); }
        }

        /// <summary>Whether there is a page to go back to, rather than only the way out.</summary>
        public bool CanGoBack
        {
            get { return _canGoBack; }
            set { Set(ref _canGoBack, value); }
        }

        /// <summary>Goes back a page, or null on the first page.</summary>
        public ICommand Back
        {
            get { return _back; }
            set { Set(ref _back, value); }
        }

        /// <summary>The words on the button at the right end of the header, or empty where the screen has none.</summary>
        /// <remarks>Separate from going back, which always means exactly what it says: return to the screen before.</remarks>
        public string PrimaryText
        {
            get { return _primaryText; }
            set { Set(ref _primaryText, value ?? ""); }
        }

        /// <summary>What the header's button does, or null while it cannot be pressed.</summary>
        public ICommand Primary
        {
            get { return _primary; }
            set { Set(ref _primary, value); }
        }

        /// <summary>The note on the header's button: what it does, or what has to happen before it can be pressed.</summary>
        public string PrimaryAbout
        {
            get { return _primaryAbout; }
            set { Set(ref _primaryAbout, value ?? ""); }
        }

        /// <summary>Whether the header has the button at all.</summary>
        public bool HasPrimary => !string.IsNullOrEmpty(_primaryText);

        /// <summary>Screen-level actions, which a target renders wherever it puts navigation.</summary>
        public IList<ChoiceEntry> Actions => _actions;

        /// <summary>A short question waiting to be answered, or empty.</summary>
        /// <remarks>
        /// Not a screen. A target draws this over whatever is already there and nothing about it moves - which is the
        /// difference between "switch this on?" and "delete this forever?", and why the second one is a page of its own.
        /// <para>
        /// While one is up, a target must offer <see cref="Answers" /> and nothing else. A question somebody can walk
        /// around is a question that gets answered by accident.
        /// </para>
        /// </remarks>
        public string Question => _question;

        /// <summary>The words for the question's named slots, or null where it has none and is read as it is.</summary>
        public Arguments QuestionArguments => _questionArguments;

        /// <summary>Whether a question is waiting: words asking it, or answers to choose from with no words over them, as a menu hanging under a part is.</summary>
        public bool HasQuestion => !string.IsNullOrEmpty(_question) || _answers.Count > 0;

        /// <summary>Whether the question belongs to whatever was pressed, rather than to the screen.</summary>
        /// <remarks>
        /// One hangs under the control it is about and reads as that control opening; the other sits in the middle and
        /// reads as the screen asking. Which it is belongs to whoever raised it, because only they know what the answers
        /// are about.
        /// </remarks>
        public bool QuestionIsHere => _questionIsHere;

        /// <summary>How many questions have been raised, so a target can tell a new one from the same one.</summary>
        /// <remarks>
        /// A count rather than the words, because two questions can read the same and a target that compared text would
        /// show the first one's answers under the second one's name.
        /// </remarks>
        public int Asked { get; private set; }

        /// <summary>The ways to answer the question, in the order they should be offered.</summary>
        public IList<ChoiceEntry> Answers => _answers;

        /// <summary>The answer Enter takes, or null where it takes none.</summary>
        /// <remarks>
        /// The page's to say and not a target's to guess, and not always the first: the order answers read best in is not
        /// the order of their safety.
        /// </remarks>
        public ChoiceEntry Default => _default;

        /// <summary>Says <see cref="Heading" /> has been filled again, so that what its parts say from now on counts as a change here.</summary>
        public void Reheaded()
        {
            foreach (var part in Heading)
                part.Show(this);

            _revision++;
        }

        /// <summary>Shows <paramref name="actions" /> in place of the actions shown until now.</summary>
        public void ShowActions(IList<ChoiceEntry> actions)
        {
            _actions.Clear();
            for (var i = 0; i < actions.Count; i++)
                _actions.Add(actions[i]);

            _revision++;
        }

        /// <summary>Raises <paramref name="question" />, with no answers yet; <paramref name="here" /> says it belongs to what was pressed.</summary>
        public void Ask(string question, bool here)
        {
            Ask(question, null, here);
        }

        /// <summary>The same, with the words for its named slots in <paramref name="arguments" />, so a name is shown as it was written.</summary>
        public void Ask(string question, Arguments arguments, bool here)
        {
            _question = question ?? "";
            _questionArguments = arguments;
            _questionIsHere = here;
            _answers.Clear();
            _default = null;
            Asked++;
            _revision++;
        }

        /// <summary>Adds <paramref name="answer" /> to the standing question, as the one Enter takes where <paramref name="byDefault" />.</summary>
        public void Answer(ChoiceEntry answer, bool byDefault)
        {
            _answers.Add(answer);
            if (byDefault)
                _default = answer;

            _revision++;
        }

        /// <summary>Takes the standing question away, answers and all.</summary>
        public void Unask()
        {
            _question = "";
            _questionArguments = null;
            _questionIsHere = false;
            _answers.Clear();
            _default = null;
            _revision++;
        }

        /// <summary>Counts a change to one of the heading's parts.</summary>
        public void Touch()
        {
            _revision++;
        }

        /// <summary>Writes <paramref name="value" /> into <paramref name="field" />, counting a revision where it differs.</summary>
        void Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            _revision++;
        }
    }
}
