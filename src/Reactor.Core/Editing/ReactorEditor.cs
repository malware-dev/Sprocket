using System;
using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Runtime;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// The editor. All of it.
    /// </summary>
    /// <remarks>
    /// Navigation, page contents, slot filling, search, the wording of every
    /// sentence and every status line live here rather than in a target. A target
    /// renders <see cref="Screen"/> and sends back command ids; it decides nothing
    /// about what the editor means or does.
    ///
    /// That split is the whole architecture. Anything a target has to re-derive is a
    /// thing that will drift between targets, and the second implementation is where
    /// you find out.
    /// </remarks>
    public sealed class ReactorEditor
    {
        readonly IWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        readonly List<EditorPage> _stack = new List<EditorPage>();
        readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();
        readonly Dictionary<int, Action<string>> _textActions = new Dictionary<int, Action<string>>();
        readonly Dictionary<int, Action<bool>> _toggleActions = new Dictionary<int, Action<bool>>();
        readonly EditorScreen _screen = new EditorScreen();

        int _nextCommand;
        int _structureVersion;
        bool _structureDirty;

        public ReactorEditor(IWorld world, Automation program, AutomationRunner runner)
        {
            _world = world;
            _program = program;
            _runner = runner;
            _stack.Add(new SequencesPage(this));
            Rebuild();
        }

        /// <summary>What to show right now.</summary>
        public EditorScreen Screen { get { return _screen; } }

        /// <summary>Beats elapsed. The only honest unit of time available.</summary>
        public int Beat { get { return _runner.Beat; } }

        /// <summary>
        /// Advances everything to the given frame and brings the screen up to date.
        /// </summary>
        /// <remarks>
        /// The clock belongs to the target, and so does the frame number: Space
        /// Engineers passes its own counter, a desktop host passes one it keeps, a
        /// test passes whatever it likes. Nothing in core counts time for itself.
        /// </remarks>
        public void Tick(int beat)
        {
            _runner.Tick(beat);

            if (_structureDirty)
            {
                Rebuild();
                return;
            }

            Current.Refresh();
            _screen.Subtitle = Current.Subtitle;
        }

        /// <summary>Acts on a button, a row, or anything else that was pressed.</summary>
        public void Invoke(int command)
        {
            Action action;
            if (!_actions.TryGetValue(command, out action))
                return;

            action();
            Settle();
        }

        /// <summary>Reports new text for a search box or any other field.</summary>
        public void SetText(int command, string value)
        {
            Action<string> action;
            if (!_textActions.TryGetValue(command, out action))
                return;

            action(value ?? "");
            Settle();
        }

        /// <summary>Reports a toggle being flipped.</summary>
        public void SetToggle(int command, bool on)
        {
            Action<bool> action;
            if (!_toggleActions.TryGetValue(command, out action))
                return;

            action(on);
            Settle();
        }

        internal IWorld World { get { return _world; } }

        internal Automation Program { get { return _program; } }

        internal AutomationRunner Runner { get { return _runner; } }

        internal EditorPage Current { get { return _stack[_stack.Count - 1]; } }

        internal int Register(Action action)
        {
            var id = ++_nextCommand;
            _actions.Add(id, action);
            return id;
        }

        internal int RegisterText(Action<string> action)
        {
            var id = ++_nextCommand;
            _textActions.Add(id, action);
            return id;
        }

        internal int RegisterToggle(Action<bool> action)
        {
            var id = ++_nextCommand;
            _toggleActions.Add(id, action);
            return id;
        }

        internal void Push(EditorPage page)
        {
            _stack.Add(page);
            _structureDirty = true;
        }

        internal void Pop()
        {
            if (_stack.Count <= 1)
                return;

            _stack.RemoveAt(_stack.Count - 1);
            _structureDirty = true;
        }

        internal void Reset(EditorPage page)
        {
            _stack.Clear();
            _stack.Add(page);
            _structureDirty = true;
        }

        /// <summary>The shape of the screen has changed and must be rebuilt.</summary>
        internal void Invalidate()
        {
            _structureDirty = true;
        }

        /// <summary>
        /// Drops any page showing the given sequence, so deleting one never leaves the
        /// user staring at something that no longer exists.
        /// </summary>
        internal void ForgetSequence(int sequenceId)
        {
            for (var i = _stack.Count - 1; i > 0; i--)
            {
                var page = _stack[i] as SequencePage;
                if (page != null && page.SequenceId == sequenceId)
                    _stack.RemoveAt(i);
            }
            _structureDirty = true;
        }

        internal bool IsRootPage<T>() where T : EditorPage
        {
            return _stack.Count > 0 && _stack[0] is T;
        }

        void Settle()
        {
            if (_structureDirty)
                Rebuild();
        }

        void Rebuild()
        {
            _actions.Clear();
            _textActions.Clear();
            _toggleActions.Clear();
            _nextCommand = 0;

            _screen.Components.Clear();
            _screen.Actions.Clear();

            var page = Current;
            _screen.Title = page.Title;
            _screen.CanGoBack = _stack.Count > 1;
            _screen.BackCommand = _screen.CanGoBack ? Register(Pop) : 0;

            page.Build(_screen);
            page.Refresh();

            _screen.Subtitle = page.Subtitle;
            BuildActions();

            _structureVersion++;
            _screen.StructureVersion = _structureVersion;
            _structureDirty = false;
        }

        void BuildActions()
        {
            // What the machine does comes first; the ship is for looking things up.
            _screen.Actions.Add(new ChoiceEntry("Sequences", IsRootPage<SequencesPage>(),
                Register(GoToSequences)));
            _screen.Actions.Add(new ChoiceEntry("Ship", IsRootPage<ShipPage>(),
                Register(GoToShip)));
            _screen.Actions.Add(new ChoiceEntry("Start something new", false,
                Register(OfferStarters)));
        }

        void GoToShip()
        {
            Reset(new ShipPage(this));
        }

        void GoToSequences()
        {
            Reset(new SequencesPage(this));
        }

        /// <summary>
        /// Asks what should start it, before anything else.
        /// </summary>
        /// <remarks>
        /// The opening question is the verb, not the thing. Someone who arrives
        /// knowing what they want thinks "when the button is pressed, open the door"
        /// - so "When" is the first word they should meet, and the blocks come after
        /// it in the order the sentence reads.
        ///
        /// Browsing the ship is still there, but as a way to look things up rather
        /// than as the only door in.
        /// </remarks>
        internal void OfferStarters()
        {
            var page = new ChoicePage(this, "What should start it?");

            page.Add("When something happens", false, delegate
            {
                StartSequence(new WhenTrigger(""));
            });

            page.Add("Every so often", false, delegate
            {
                StartSequence(new EveryTrigger(Cadences.Default));
            });

            Push(page);
        }

        /// <summary>Creates a sequence and opens it, leaving no chooser behind.</summary>
        internal void StartSequence(Trigger trigger)
        {
            if (Current is ChoicePage)
                Pop();

            Push(new SequencePage(this, _program.AddSequence(trigger)));
        }
    }
}
