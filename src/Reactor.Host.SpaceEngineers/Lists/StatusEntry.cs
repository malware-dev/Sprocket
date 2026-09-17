using Reactor.Host.SpaceEngineers.Screen;
namespace Reactor.Host.SpaceEngineers.Lists
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
        /// How far in this row sits: 0 for a thing, 1 for one of that thing.
        /// </summary>
        /// <remarks>
        /// One level, and there will never be more. A system and the ones of it fitted to a
        /// ship is the whole of the nesting this language has, so a target can indent by a
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
            set { Set(ref _switchIsOn, value, "SwitchIsOn"); }
        }

        /// <summary>Read as one line - a name, or a whole sentence.</summary>
        public string Summary
        {
            get { return _summary; }
            set { Set(ref _summary, value, "Summary"); }
        }

        /// <summary>Live: waiting, running, switched off.</summary>
        public string Status
        {
            get { return _status; }
            set { Set(ref _status, value, "Status"); }
        }

        /// <summary>When it last ran, in words. Empty if it never has.</summary>
        public string LastRun
        {
            get { return _lastRun; }
            set { Set(ref _lastRun, value, "LastRun"); }
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
            set { Set(ref _ranLight, value, "JustRan"); }
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
            set { Set(ref _problemIsWarning, value, "ProblemIsWarning"); }
        }

        bool _problemIsWarning;

        public int Problems
        {
            get { return _problems; }
            set { Set(ref _problems, value, "Problems"); }
        }

        public string Problem
        {
            get { return _problem; }
            set { Set(ref _problem, value, "Problem"); }
        }
    }
}
