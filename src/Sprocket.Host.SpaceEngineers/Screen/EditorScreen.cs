using System.Collections.Generic;
using System.ComponentModel;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Lists;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Everything on screen right now.
    /// </summary>
    /// <remarks>
    /// One screen at a time, with a back stack. That is a deliberate floor rather
    /// than a limitation: the editor has to work wherever it is embedded, and a
    /// layout assuming room for a sidebar plus a detail pane plus a sequence list is an
    /// assumption the smallest target cannot honor. A roomy target is free to render
    /// two levels of the stack side by side - nothing in core depends on it.
    /// </remarks>
    public sealed class EditorScreen : Observable
    {
        public EditorScreen()
        {
            Components = new ObservableList<EditorComponent>();
            Actions = new ObservableList<ChoiceEntry>();
            Answers = new ObservableList<ChoiceEntry>();
            PrimaryFields = new ObservableList<Field>();
            PrimaryFilled = new ObservableList<string>();
            Question = "";
            Heading = new List<HeadingPart>();
        }

        /// <summary>
        /// What this screen is called, in order, one part at a time.
        /// </summary>
        /// <remarks>
        /// Never empty: every screen has at least one part, and most have exactly one. A sequence has two
        /// because it is two things - what starts it, and what it then does - and naming the second used
        /// to replace both.
        /// <para>
        /// <b>There is deliberately no joined-up string of the whole thing here.</b> One was easy to add
        /// and every target would have drawn it, because drawing one string is easier than drawing several
        /// - and the whole reason this is a list is that a heading has to show which part of it is
        /// somebody's to change. An API that offers the easy wrong thing gets the easy wrong thing.
        /// </para>
        /// </remarks>
        public List<HeadingPart> Heading { get; private set; }

        /// <summary>Context the title cannot carry - a count, a live status.</summary>
        public string Subtitle
        {
            get { return _subtitle; }
            set { Set(ref _subtitle, value, nameof(Subtitle)); }
        }

        string _subtitle;

        /// <summary>Whether any part of the heading is somebody's to change.</summary>
        /// <remarks>
        /// Naming belongs on the heading because the heading is what the name is for. It replaced a
        /// naming box that sat inside the sequence, under a heading that already showed the name - two
        /// places for one thing, one announcing it and one setting it. Whether it is a pencil beside the
        /// words, a long press, or typing straight over them is the target's business.
        /// </remarks>
        public bool CanRename
        {
            get
            {
                for (var i = 0; i < Heading.Count; i++)
                {
                    if (Heading[i].CanRename)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Reads everything the host can only learn by looking, and says so on screen.
        /// </summary>
        /// <remarks>
        /// Every screen has one, because everything a screen shows about what is wired was read at some
        /// moment and nothing tells Sprocket when a slot changes - least of all when the person who
        /// changed it is another player on the same ship.
        /// </remarks>
        public int RefreshCommand
        {
            get { return _refreshCommand; }
            set { Set(ref _refreshCommand, value, nameof(RefreshCommand)); }
        }

        int _refreshCommand;

        /// <summary>
        /// Switches this screen's subject on or off. Zero when it has no such switch.
        /// </summary>
        /// <remarks>
        /// A system's switch, where a sequence's lives on the sentence itself. Same idea at a
        /// larger scale: rewiring an airlock means stopping the airlock, not stopping three
        /// sequences one at a time - and a half-stopped one, with the inner door's rule off
        /// and the outer door's rule still live, is worse than either state.
        /// </remarks>
        public int SwitchCommand
        {
            get { return _switchCommand; }
            set { Set(ref _switchCommand, value, nameof(SwitchCommand)); }
        }

        int _switchCommand;

        public bool HasSwitch => SwitchCommand != 0;

        /// <summary>Whether it is currently allowed to run.</summary>
        public bool SwitchIsOn
        {
            get { return _switchIsOn; }
            set { Set(ref _switchIsOn, value, nameof(SwitchIsOn)); }
        }

        bool _switchIsOn;

        /// <summary>
        /// Whether this screen has a way back from what was done on it.
        /// </summary>
        /// <remarks>
        /// Separate from having anything to walk back right now, because the pair holds its place
        /// either way: arrows that appeared with the first edit would shift the switch beside them
        /// out from under the pointer that had just made it.
        /// </remarks>
        public bool HasHistory
        {
            get { return _hasHistory; }
            set { Set(ref _hasHistory, value, nameof(HasHistory)); }
        }

        bool _hasHistory;

        /// <summary>
        /// Takes back the last change made on this screen. Zero when there is nothing to take back.
        /// </summary>
        /// <remarks>
        /// Screen furniture, beside Back and the switch, because that is what it is: the way back
        /// from what you did to the thing you are looking at. A sequence's own pair sits on its
        /// sentence instead, among the bin and the switch that belong to that sentence.
        /// </remarks>
        public int UndoCommand
        {
            get { return _undoCommand; }
            set { Set(ref _undoCommand, value, nameof(UndoCommand)); }
        }

        int _undoCommand;

        /// <summary>
        /// What undoing would undo, e.g. "deleting a trigger".
        /// </summary>
        /// <remarks>
        /// "Undo" alone asks somebody to remember what they last did, which is exactly what a person
        /// who has just made a mistake is worst at.
        /// </remarks>
        public string UndoText
        {
            get { return _undoText; }
            set { Set(ref _undoText, value, nameof(UndoText)); }
        }

        string _undoText;

        /// <summary>Puts back the last thing undone. Zero when there is nothing waiting.</summary>
        public int RedoCommand
        {
            get { return _redoCommand; }
            set { Set(ref _redoCommand, value, nameof(RedoCommand)); }
        }

        int _redoCommand;

        /// <summary>What redoing would put back, in the words it happened in.</summary>
        public string RedoText
        {
            get { return _redoText; }
            set { Set(ref _redoText, value, nameof(RedoText)); }
        }

        string _redoText;

        public bool CanGoBack
        {
            get { return _canGoBack; }
            set { Set(ref _canGoBack, value, nameof(CanGoBack)); }
        }

        bool _canGoBack;

        public int BackCommand
        {
            get { return _backCommand; }
            set { Set(ref _backCommand, value, nameof(BackCommand)); }
        }

        int _backCommand;

        /// <summary>
        /// The obvious next move, if this screen has one - "Next", "Done".
        /// </summary>
        /// <remarks>
        /// Deliberately separate from going back, which always means exactly what it
        /// says: return to the screen you came from. Overloading back with "finished"
        /// breaks the one navigation idea everybody already has, and leaves someone
        /// who genuinely wants to retreat with nowhere to press.
        ///
        /// Empty when there is nothing to move on to - which on a picker also means
        /// "you have not chosen anything yet".
        /// </remarks>
        public string PrimaryText
        {
            get { return _primaryText; }
            set { Set(ref _primaryText, value, nameof(PrimaryText)); }
        }

        string _primaryText;

        public int PrimaryCommand
        {
            get { return _primaryCommand; }
            set { Set(ref _primaryCommand, value, nameof(PrimaryCommand)); }
        }

        int _primaryCommand;

        public bool HasPrimary => !string.IsNullOrEmpty(PrimaryText);

        /// <summary>
        /// What pressing it asks for, or empty when pressing it just does the thing.
        /// </summary>
        /// <remarks>
        /// A screen that needs a value before it can finish asks for it on the button that finishes
        /// it, rather than moving somebody to a screen with a field on it. One press, the field
        /// hangs under what they pressed, and the row they were working on stays in sight - which is
        /// the same rule every other question on these screens follows.
        /// </remarks>
        public string PrimaryQuestion
        {
            get { return _primaryQuestion; }
            set { Set(ref _primaryQuestion, value, nameof(PrimaryQuestion)); }
        }

        string _primaryQuestion;

        public bool PrimaryAsks => !string.IsNullOrEmpty(PrimaryQuestion);

        /// <summary>
        /// Whether the question belongs to whatever was pressed, rather than to the screen.
        /// </summary>
        /// <remarks>
        /// One hangs under the control it is about and reads as that control opening; the other sits
        /// in the middle and reads as the screen asking. Which it is belongs to whoever raised it,
        /// because only they know what the answers are about.
        /// </remarks>
        public bool QuestionIsHere
        {
            get { return _questionIsHere; }
            set { Set(ref _questionIsHere, value, nameof(QuestionIsHere)); }
        }

        bool _questionIsHere;

        /// <summary>
        /// What each value pressing it asks for wants, one field each.
        /// </summary>
        /// <remarks>
        /// One box holding three things is a separator to get wrong, so there is a field per value
        /// - with a name beside it, and what it will take, wherever anybody has said what that
        /// value is. Nothing here decides any of it: it comes from what the thing being answered
        /// declared.
        /// </remarks>
        public ObservableList<Field> PrimaryFields { get; private set; }

        /// <summary>What to show, in order.</summary>
        public ObservableList<EditorComponent> Components { get; private set; }

        /// <summary>
        /// Screen-level actions: the roots to switch between, and starting a sequence
        /// from scratch. A target renders these wherever it puts navigation.
        /// </summary>
        public ObservableList<ChoiceEntry> Actions { get; private set; }

        /// <summary>
        /// A short question waiting to be answered, or empty.
        /// </summary>
        /// <remarks>
        /// Not a screen. A target draws this over whatever is already there and nothing about it moves
        /// - which is the difference between "switch this on?" and "delete this forever?", and why the
        /// second one is still a page of its own.
        /// <para>
        /// While one is up, a target must offer <see cref="Answers" /> and nothing else. Core cannot
        /// enforce that, and a question somebody can walk around is a question that gets answered by
        /// accident.
        /// </para>
        /// </remarks>
        public string Question
        {
            get { return _question; }
            set { Set(ref _question, value, nameof(Question)); }
        }

        string _question;

        public bool HasQuestion => !string.IsNullOrEmpty(Question);

        /// <summary>
        /// How many questions have been raised, so a target can tell a new one from the same one.
        /// </summary>
        /// <remarks>
        /// A count rather than the words, because two questions can read the same and a target that
        /// compared text would show the first one's answers under the second one's name. It only has
        /// to differ from last time, never to mean anything.
        /// </remarks>
        public int Asked { get; internal set; }

        /// <summary>
        /// Which answer means "finish this page", or zero where none does.
        /// </summary>
        /// <remarks>
        /// Said apart from the others because finishing is not always one act. A page that cannot
        /// finish without values has to ask for them, and that question is a target's to open - so
        /// an answer that stands for finishing has to reach the same place the button does rather
        /// than doing half of what it does.
        /// </remarks>
        public int Finishing { get; internal set; }

        /// <summary>What the primary's fields open holding, one per field, or empty for none.</summary>
        public ObservableList<string> PrimaryFilled { get; private set; }

        /// <summary>The ways to answer it, in the order they should be offered.</summary>
        /// <remarks>The first is the one a reflex press should land on, so it is the safe one.</remarks>
        public ObservableList<ChoiceEntry> Answers { get; private set; }

        /// <summary>
        /// Which answer Enter takes, or zero where it takes none.
        /// </summary>
        /// <remarks>
        /// The page's to say and not a target's to guess, and not always the first row: a question
        /// whose safe answer is "keep it" and one whose safe answer is "carry on" both exist, and
        /// the order they read best in is not the order of their safety.
        /// </remarks>
        public int Default
        {
            get { return _default; }
            set { Set(ref _default, value, nameof(Default)); }
        }

        int _default;

        /// <summary>The parts this is repeating for, so each one can be let go of again.</summary>
        readonly List<HeadingPart> _heard = new List<HeadingPart>();

        /// <summary>
        /// Says the heading has been built again, and repeats what its parts say from now on.
        /// </summary>
        /// <remarks>
        /// <see cref="Heading" /> is a plain list, rebuilt in place so that a target holding it keeps
        /// hold of it - and a list rebuilt in place announces nothing, so this is the one place the
        /// screen has to be told out loud. Core calls it where it rebuilds the heading.
        /// <para>
        /// The parts themselves change afterwards: a page reads the grid's name and the words of a
        /// sentence on a cadence, and writes them here. So they are listened to, and let go of when the
        /// list is built again - a part still listened to after it has gone would keep this screen
        /// hearing about a heading nobody is showing.
        /// </para>
        /// </remarks>
        internal void Renamed()
        {
            for (int i = 0; i < _heard.Count; i++)
                _heard[i].PropertyChanged -= Said;

            _heard.Clear();

            for (int i = 0; i < Heading.Count; i++)
            {
                Heading[i].PropertyChanged += Said;
                _heard.Add(Heading[i]);
            }

            Raise(nameof(Heading));
        }

        void Said(object sender, PropertyChangedEventArgs e)
        {
            // Coarsely: the heading is different now. Which part and which word is detail nothing
            // here would do anything with.
            Raise(nameof(Heading));
        }
    }
}
