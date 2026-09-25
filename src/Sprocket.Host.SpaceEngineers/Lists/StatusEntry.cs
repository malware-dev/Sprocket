using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>One thing that is doing something, summarised to a line.</summary>
    public sealed class StatusEntry : Observable
    {
        string _summary;
        string _status;
        string _lastRun;
        string _problem;
        bool _ranLight;
        int _problems;

        public StatusEntry(string summary, int command)
        {
            _summary = summary;
            Command = command;
        }

        /// <summary>
        /// Throwing away what this row is, or zero where that is not offered.
        /// </summary>
        /// <remarks>
        /// For rows whose loss is nothing: a trigger is one line and three presses to make again, so
        /// it goes without a question. A sequence is work, which is why its bin is on its own screen
        /// and asks first.
        /// </remarks>
        /// <summary>
        /// Runs what this row is about, now, by hand. Zero where it may not run.
        /// </summary>
        /// <remarks>
        /// The same act as its own screen offers, on the row, because this is the list somebody is
        /// looking at when they want to see what a sequence does - and what starts it is usually a
        /// button somewhere else on the ship.
        /// </remarks>
        /// <summary>
        /// How many of the things below this row have something to remark on.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Problems" /> because it is said in a different colour and must
        /// not be mistaken for one. A row with both says the louder of the two.
        /// </remarks>
        public int Remarks
        {
            get { return _remarks; }
            set { Set(ref _remarks, value, nameof(Remarks)); }
        }

        int _remarks;

        /// <summary>Ending the run that is going, which takes the place of starting one.</summary>
        public int StopCommand
        {
            get { return _stopCommand; }
            set { Set(ref _stopCommand, value, nameof(StopCommand)); }
        }

        int _stopCommand;

        public int RunCommand
        {
            get { return _runCommand; }
            set { Set(ref _runCommand, value, nameof(RunCommand)); }
        }

        int _runCommand;

        /// <summary>
        /// Opens what the row's second line says, where that is a thing of its own to change.
        /// </summary>
        /// <remarks>
        /// A trigger's row reads as two sentences - when it happens, and what it then does - and
        /// either may be what somebody came to change. Two presses on one row rather than a question
        /// asking which they meant: the answer to that question is already on the row, in the words
        /// they are looking at.
        /// </remarks>
        public int StatusCommand
        {
            get { return _statusCommand; }
            set { Set(ref _statusCommand, value, nameof(StatusCommand)); }
        }

        int _statusCommand;

        public int DeleteCommand
        {
            get { return _deleteCommand; }
            set { Set(ref _deleteCommand, value, nameof(DeleteCommand)); }
        }

        int _deleteCommand;

        /// <summary>
        /// Making another of what this row is, or zero where that is not offered.
        /// </summary>
        /// <remarks>
        /// On the row rather than inside the thing, because wanting another one like this is
        /// something somebody decides while reading the list.
        /// </remarks>
        public int DuplicateCommand
        {
            get { return _duplicateCommand; }
            set { Set(ref _duplicateCommand, value, nameof(DuplicateCommand)); }
        }

        int _duplicateCommand;

        /// <summary>
        /// Whether this is running at this instant.
        /// </summary>
        /// <remarks>
        /// Not <see cref="JustRan" />, which is a flash saying something happened a moment ago. This
        /// is a situation and lasts as long as it lasts, which is why it is worth a ground of its own
        /// rather than a light: a row somebody is looking for is the one that is going now.
        /// </remarks>
        public bool Running
        {
            get { return _running; }
            set { Set(ref _running, value, nameof(Running)); }
        }

        bool _running;

        /// <summary>
        /// Whether this has been switched off, which is what the ground says.
        /// </summary>
        /// <remarks>
        /// The commonest reason a button does nothing, and the one where everything else about the
        /// row reads as working. The words for it are in <see cref="Status" />, which is the line
        /// the row already has for what it is doing and is the same fact - so nothing appears or
        /// vanishes as the switch beside it is pressed, and the row cannot move out from under the
        /// press that moved it.
        /// </remarks>
        public bool Parked
        {
            get { return _parked; }
            set { Set(ref _parked, value, nameof(Parked)); }
        }

        bool _parked;

        /// <summary>
        /// What copying this would do, in words, for the note on the button.
        /// </summary>
        /// <remarks>
        /// Core's words rather than the target's, the same as <c>LockedText</c> on a sentence: what a
        /// copy arrives with is a decision of the model, and the consequence of it - that the copy has
        /// to be told what starts it - is not something a target could work out.
        /// </remarks>
        public string DuplicateText
        {
            get { return _duplicateText; }
            set { Set(ref _duplicateText, value ?? "", nameof(DuplicateText)); }
        }

        string _duplicateText = "";

        /// <summary>What pressing the row's button does, or zero where the row has none.</summary>
        /// <remarks>
        /// One button in words, for the act on a row that is the way on to something new and has to be
        /// found without being looked for: a mark with a note under it is only found by pointing at it.
        /// </remarks>
        public int ButtonCommand
        {
            get { return _buttonCommand; }
            set { Set(ref _buttonCommand, value, nameof(ButtonCommand)); }
        }

        int _buttonCommand;

        /// <summary>What the row's button says.</summary>
        public string ButtonText
        {
            get { return _buttonText; }
            set { Set(ref _buttonText, value ?? "", nameof(ButtonText)); }
        }

        string _buttonText = "";

        /// <summary>What the row's button asks before it is done, or empty where it asks nothing.</summary>
        public string ButtonQuestion
        {
            get { return _buttonQuestion; }
            set { Set(ref _buttonQuestion, value ?? "", nameof(ButtonQuestion)); }
        }

        string _buttonQuestion = "";

        /// <summary>The fields the row's button asks for, answered together; none for one answer.</summary>
        public List<Field> ButtonFields { get; } = new List<Field>();

        /// <summary>What <see cref="ButtonFields" /> open holding, or none for empty ones.</summary>
        public List<string> ButtonFilled { get; } = new List<string>();

        /// <summary>Whether this is the row of a list that the rest of its screen is about.</summary>
        /// <remarks>The logic system on a blueprint's screen whose blocks are being shown beside it.</remarks>
        public bool Chosen
        {
            get { return _chosen; }
            set { Set(ref _chosen, value, nameof(Chosen)); }
        }

        bool _chosen;

        /// <summary>What pressing the row asks before it is done, or empty where it asks nothing.</summary>
        /// <remarks>For a row that adds something, which is named as it is made.</remarks>
        public string Question
        {
            get { return _question; }
            set { Set(ref _question, value ?? "", nameof(Question)); }
        }

        string _question = "";

        /// <summary>
        /// How far in this row sits: 0 for a thing, 1 for one of that thing.
        /// </summary>
        /// <remarks>
        /// One level, and there will never be more. A system and its instances is the whole of
        /// the nesting this language has, so a target can indent by a
        /// fixed amount rather than reinventing a tree.
        /// </remarks>
        public int Depth
        {
            get { return _depth; }
            set { Set(ref _depth, value, nameof(Depth)); }
        }

        int _depth;

        /// <summary>
        /// Whether this row does something rather than being something.
        /// </summary>
        /// <remarks>
        /// "Another one of these" belongs in the list, under the system it would add to,
        /// rather than somewhere else on the screen where it would have to explain which
        /// system it meant. It is not a thing with a state, so a target should draw it
        /// quieter than its neighbours.
        /// </remarks>
        public bool IsAction
        {
            get { return _isAction; }
            set { Set(ref _isAction, value, nameof(IsAction)); }
        }

        bool _isAction;

        public int Command { get; private set; }

        /// <summary>
        /// Switches this row's subject on or off, or zero when it has no switch.
        /// </summary>
        /// <remarks>
        /// A system can be stopped from the list it appears in, without opening it. Somebody
        /// welding on a hangar wants that hangar left alone, and making them walk into it to
        /// say so is a step that buys nothing - the list is where they are already looking.
        /// </remarks>
        public int SwitchCommand
        {
            get { return _switchCommand; }
            set { Set(ref _switchCommand, value, nameof(SwitchCommand)); }
        }

        int _switchCommand;

        public bool CanSwitch => SwitchCommand != 0;

        /// <summary>
        /// Whether this row's switch governs everything nested under it, or only itself.
        /// </summary>
        /// <remarks>
        /// A fact about what the switch does, not about how to draw it. Two switches in a
        /// column, one over the whole thing and one over a part of it, are worth telling
        /// apart, and only core knows which is which - a target reading the indent would be
        /// guessing from a layout it chose itself.
        /// </remarks>
        public bool SwitchGovernsAll
        {
            get { return _switchGovernsAll; }
            set { Set(ref _switchGovernsAll, value, nameof(SwitchGovernsAll)); }
        }

        bool _switchGovernsAll;

        bool _switchIsOn;

        /// <summary>Whether it is currently allowed to run.</summary>
        public bool SwitchIsOn
        {
            get { return _switchIsOn; }
            set { Set(ref _switchIsOn, value, nameof(SwitchIsOn)); }
        }

        /// <summary>Read as one line - a name, or a whole sentence.</summary>
        public string Summary
        {
            get { return _summary; }
            set { Set(ref _summary, value, nameof(Summary)); }
        }

        /// <summary>Live: waiting, running, switched off.</summary>
        public string Status
        {
            get { return _status; }
            set { Set(ref _status, value, nameof(Status)); }
        }

        /// <summary>When it last ran, in words. Empty if it never has.</summary>
        public string LastRun
        {
            get { return _lastRun; }
            set { Set(ref _lastRun, value, nameof(LastRun)); }
        }

        /// <summary>
        /// Whether it ran a moment ago.
        /// </summary>
        /// <remarks>
        /// The fact, not the flash. A target that wants a heartbeat has one; a target that
        /// would rather say so in words is not being told to blink.
        /// </remarks>
        public bool JustRan
        {
            get { return _ranLight; }
            set { Set(ref _ranLight, value, nameof(JustRan)); }
        }

        /// <summary>
        /// How many things have something wrong with them at or below this row.
        /// </summary>
        /// <remarks>
        /// A count rather than a mark, and it travels upward: the only way to find a broken sequence
        /// was to open every system and look, so every row on the way now carries the number of
        /// broken things it leads to. The row that owns the problem counts itself, and says what is
        /// wrong in <see cref="Problem" /> as well - one number for where, one line for what.
        /// <para>
        /// A target draws an exclamation, a colour, the number, or nothing; core only counts. It has
        /// to draw something, though, because this is the whole of how somebody finds the one broken
        /// row among forty.
        /// </para>
        /// </remarks>
        /// <summary>Whether <see cref="Problem" /> is a warning rather than something broken.</summary>
        public bool ProblemIsWarning
        {
            get { return _problemIsWarning; }
            set { Set(ref _problemIsWarning, value, nameof(ProblemIsWarning)); }
        }

        bool _problemIsWarning;

        public int Problems
        {
            get { return _problems; }
            set { Set(ref _problems, value, nameof(Problems)); }
        }

        public string Problem
        {
            get { return _problem; }
            set { Set(ref _problem, value, nameof(Problem)); }
        }
    }
}
