using Sprocket.Host.SpaceEngineers.Features.Codex;
using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Storage;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>Picks blocks and then what happens to them, with both stages on screen at once.</summary>
    /// <remarks>
    /// Side by side, the whole path reads at a glance and changing an earlier answer is a glance to the left. The page keeps
    /// its own answer and hands it back when somebody says they are done: ticking entries straight into a step would make a
    /// tentative tap an edit to a sequence that might be running, and would leave back with nothing to back out of. The
    /// strip it is drawn in is declared in <c>PickPage.View.cs</c>, and shared with the page before or after it in the same
    /// pick; this half is what it asks and what each answer does.
    /// </remarks>
    public sealed partial class PickPage : EditorPage
    {
        /// <summary>What a test's choice starts with where it is for its blocks not being in the state it names.</summary>
        public const string Not = "!";

        const bool AsksAll = false;
        const string FamilyPrefix = "family:";
        const int Actions = 0;
        const int Values = 1;

        static readonly NameOrder ByName = new NameOrder();
        static readonly SaidOrder BySaid = new SaidOrder(false);
        static readonly SaidOrder ByDeclared = new SaidOrder(true);

        readonly string _title;
        readonly List<long> _selection = new List<long>();
        readonly bool _forEvents;
        readonly bool _forTests;
        readonly bool _asksAll;
        readonly Action<PickAnswer> _done;
        readonly List<GridEntity> _candidates = new List<GridEntity>();
        readonly List<GridEntity> _filtered = new List<GridEntity>();
        readonly List<long> _filteredIds = new List<long>();
        readonly HashSet<long> _wasShown = new HashSet<long>();
        readonly List<EntityKind> _kinds = new List<EntityKind>();
        readonly EditorPage _screen;
        readonly List<EntityKind> _shownKinds = new List<EntityKind>();
        readonly List<Family> _shownFamilies = new List<Family>();
        readonly List<string> _kindKeys = new List<string>();
        readonly Dictionary<Family, string> _familyKeys = new Dictionary<Family, string>();
        readonly List<string> _names = new List<string>();
        readonly List<string> _ids = new List<string>();
        readonly List<int> _matched = new List<int>();
        readonly List<Form> _forms = new List<Form>();
        readonly List<int> _groups = new List<int>();
        readonly List<string> _marks = new List<string>();
        readonly List<bool> _sets = new List<bool>();
        readonly List<Readiness> _ready = new List<Readiness>();
        readonly List<string> _why = new List<string>();
        readonly List<int> _rows = new List<int>();
        readonly List<string> _whatKeys = new List<string>();
        readonly Dictionary<string, int> _whatAt = new Dictionary<string, int>();
        readonly List<long> _arrived = new List<long>();
        readonly List<long> _saidBlocks = new List<long>();
        readonly List<Literal> _arrivedValues = new List<Literal>();
        readonly Opening _opens;
        readonly Aim _aim;
        readonly int _base;
        readonly string _arrivedChosen;
        readonly Form _arrivedForm;
        readonly bool _arrivedAll;
        readonly List<string> _filled = new List<string>();
        readonly List<Field> _nothing = new List<Field>();
        readonly List<Field> _shownFields = new List<Field>();
        readonly Func<GridEntity, bool> _fits;
        readonly Func<GridEntity, bool> _admits;
        readonly bool _signalsOnly;
        readonly Dictionary<long, string> _ahead = new Dictionary<long, string>();

        string _chosen;
        Form _chosenForm;
        string _dropped = "";
        Form _droppedForm;
        bool _all;
        string _kind = "";
        Family _family;
        string _narrowedSearch;
        string _narrowedKind;
        string _kindSearch = "";
        string _whatSearch = "";
        string _said;
        string _saidChosen = "";
        int _saidOffered;
        bool _saidAll;
        bool _retitled;
        bool _goesOn;
        bool _straight;
        string _entered;
        string _shown;
        bool _openedArguments;
        long _anchor;

        PickStrip _strip;
        StageColumn _blocks;
        StageColumn _what;
        StageColumn _does;
        Keyed<long, PickRow> _blockRows;
        Keyed<string, KindRow> _kindRows;
        Keyed<string, PickRow> _whatRows;
        OptionRow _any;
        OptionRow _every;
        ICommand _clearKind;
        ICommand _toggleHidden;
        bool _showsHidden;
        ICommand _organized;
        ICommand _terminal;
        ICommand _finish;

        /// <summary>Asks what to point at and what should happen to it, starting from whatever the answer is now.</summary>
        /// <remarks>
        /// <paramref name="after" /> is the strip of the pick this page goes on in, as its next page, or null for a pick of its
        /// own. <paramref name="admits" /> says which of the blocks able to do what is asked are listed, or null for all of
        /// them. <paramref name="signalsOnly" /> offers only what the construct does to start something, for a signal slot.
        /// <paramref name="forTests" /> offers what an if can test: the states its blocks can be in or not, each a choice of
        /// its own marked with <see cref="Not" /> for not, and their values. Opened the way the last picker of its sort was left: what is on this construct is not known yet, so a kind it does
        /// not have is dropped later, where it is.
        /// </remarks>
        public PickPage(SprocketEditor editor, string title, bool forEvents,
            IList<long> targets, string chosen, Form chosenForm, Action<PickAnswer> done,
            bool asksAll = false, bool all = false, IList<Literal> arguments = null,
            Opening opens = Opening.Blocks, Aim aim = null, PickStrip after = null, Func<GridEntity, bool> admits = null,
            bool signalsOnly = false, bool forTests = false)
            : base(editor)
        {
            _forTests = forTests;
            _admits = admits;
            _signalsOnly = signalsOnly;
            _opens = opens;
            _aim = aim;
            if (aim != null)
            {
                aim.Onward = Primary;
                aim.MovesOn = MoveOn;
            }

            _strip = after ?? new PickStrip(editor.Texts);
            _base = after == null ? 0 : after.Stages.Count;

            if (arguments != null)
            {
                for (var i = 0; i < arguments.Count; i++)
                    _arrivedValues.Add(arguments[i]);
            }

            _title = title;
            _forEvents = forEvents;
            _chosen = chosen ?? "";
            _chosenForm = chosenForm;
            _arrivedChosen = _chosen;
            _arrivedForm = chosenForm;
            _arrivedAll = all;
            _asksAll = asksAll;
            _all = all;
            _done = done;

            _screen = editor.Opener;

            for (var i = 0; i < targets.Count; i++)
            {
                _selection.Add(targets[i]);
                _arrived.Add(targets[i]);
            }

            // A step that flips a switch was made by choosing toggle on the switch's value, so that is what it opens on.
            var flipped = !forEvents && !forTests && chosenForm == Form.Action
                ? Vocabulary.Flipped(editor.World, _selection, _chosen)
                : null;
            if (flipped != null)
            {
                _chosen = flipped;
                _arrivedChosen = flipped;
                _chosenForm = Form.Value;
                _arrivedForm = Form.Value;
                _arrivedValues.Clear();
                _arrivedValues.Add(Literal.Of(EntityAction.Toggle));
            }
        }

        /// <summary>Asks which blocks fill a slot: the blocks alone, only those <paramref name="fits" /> allows, starting from <paramref name="targets" />.</summary>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, Func<GridEntity, bool> fits,
            Action<PickAnswer> done)
            : this(editor, title, false, targets, "", Form.Native, done)
        {
            _fits = fits;
        }

        /// <summary>The title as the answer stands, or the picker's own name where nothing is chosen.</summary>
        public override string Title => _said ?? _title;

        /// <inheritdoc />
        public override string Help => Terms.Picking;

        /// <inheritdoc />
        public override bool Asks => true;

        /// <inheritdoc />
        /// <remarks>Nothing where what the trigger does goes on to a page of its own.</remarks>
        public override string PrimaryText => _aim == null || _aim.Outcome != Outcome.One ? Editor.Texts.ChromeDone : "";

        /// <inheritdoc />
        public override bool CanPrimary => IsComplete && (_aim == null || _aim.Finishes);

        /// <inheritdoc />
        public override string PrimaryAbout => Editor.Texts.PickDoneAbout;

        /// <inheritdoc />
        public override string PrimaryWaits => Editor.Texts.PickDoneFirst;

        /// <summary>What each value the chosen action takes wants, one field each, and empty where it takes none.</summary>
        public IList<Field> Fields => Wanted();

        /// <summary>Blocks listed ahead of the rest, by entity id, each with what is said of it in place of its kind.</summary>
        public IDictionary<long, string> Ahead => _ahead;

        /// <summary>What the values open holding, where this opened on something that has values.</summary>
        /// <remarks>
        /// A picker opened to change which blocks a step acts on already has that step's values, and opening them empty would
        /// make somebody type them again to keep them. Only for what the step did: another action's values are not these,
        /// whatever their count happens to be.
        /// </remarks>
        public IList<string> Filled
        {
            get
            {
                _filled.Clear();
                if (_arrivedValues.Count > 0 && _chosen == _arrivedChosen && _chosenForm == _arrivedForm)
                    Channels.Shown(_arrivedValues, Wanted(), _filled);

                return _filled;
            }
        }

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The strip the pick is drawn in, shared with the pages before and after this one.</summary>
        public PickStrip Strip => _strip;

        /// <summary>Whether anything here differs from what was chosen when it opened: the blocks, what is done to them, any or all, or the values.</summary>
        bool Moved
        {
            get
            {
                if (_selection.Count != _arrived.Count)
                    return true;

                for (var i = 0; i < _selection.Count; i++)
                {
                    if (!Contains(_arrived, _selection[i]))
                        return true;
                }

                if (_chosen != _arrivedChosen || _chosenForm != _arrivedForm || _all != _arrivedAll)
                    return true;

                if (_aim != null && _aim.Moved)
                    return true;

                return ValuesMoved;
            }
        }

        /// <summary>Whether the values hold anything other than what the step was given.</summary>
        /// <remarks>By value rather than by spelling: a step given 5 opens showing 5.0, which is the same value and no change.</remarks>
        bool ValuesMoved
        {
            get
            {
                if (_entered == null)
                    return false;

                var entered = new List<Literal>();
                ValueLine.Read(_entered, entered);
                if (entered.Count != _arrivedValues.Count)
                    return true;

                for (var i = 0; i < entered.Count; i++)
                {
                    if (!Same(entered[i], _arrivedValues[i]))
                        return true;
                }

                return false;
            }
        }

        /// <summary>Whether the blocks hidden from toolbars are listed: the eye open.</summary>
        /// <remarks>The player's own, the same in every picker, where the machine keeps preferences; this picker's alone where it does not.</remarks>
        bool ShowsHidden
        {
            get { return Editor.Preferences != null ? Editor.Preferences.ShowsHidden : _showsHidden; }
            set
            {
                if (Editor.Preferences != null)
                    Editor.Preferences.ShowsHidden = value;
                else
                    _showsHidden = value;
            }
        }

        bool IsComplete => _selection.Count > 0 && (_fits != null || !string.IsNullOrEmpty(_chosen)
            && ReadinessOf(_chosen, _chosenForm) == Readiness.Ready
            && (_aim == null || _aim.Chosen));

        /// <summary>The stages this page asks, which are the last in the pick while it is on top.</summary>
        int Own => (_blocks == null ? 0 : 1) + (_what == null ? 0 : 1) + (_does == null ? 0 : 1);

        /// <summary>The stage whose box takes the keyboard when its page is first shown, or minus one where none does.</summary>
        int Opens => _opens == Opening.Blocks ? _base : _opens == Opening.What ? _base + 1 : -1;

        /// <inheritdoc />
        public override bool TakeRetitle()
        {
            Reword();

            var retitled = _retitled;
            _retitled = false;
            return retitled;
        }

        /// <summary>Asks before throwing away what was picked here, where there is something to lose and something to do with it.</summary>
        /// <remarks>
        /// Back means back, and both answers go back: the question is only about what survives it. So the one Enter takes is
        /// the one that keeps the work, and keeping is finishing, with the values held to what they take exactly as Done holds
        /// them. Back retraces the way here: through the pick where it was paged through, and out of it where it was opened
        /// straight on this page.
        /// </remarks>
        public override bool Leaving()
        {
            if (_base > 0)
                return _straight ? LeavingStraight() : true;

            var after = _aim == null ? null : _aim.Following;
            if (!IsComplete || !Moved && (after == null || !after.Moved))
                return true;

            Editor.Ask(Editor.Texts.PickKeepQuestion);
            Editor.AnswerPrimary(Editor.Texts.PickKeepThem, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, delegate { Editor.Pop(); });

            return false;
        }

        /// <summary>Finishes, with the values given in the column beside the stages where what is chosen takes any.</summary>
        /// <remarks>Every value is held to what it takes from here on, and where one fails the column says why and the keyboard goes to it.</remarks>
        public override void Primary()
        {
            var arguments = _strip.Arguments;
            if (_forEvents || !_strip.ShowsArguments || arguments == null || arguments.Form.Count == 0)
            {
                _done(new PickAnswer(_selection, _chosen, _all, null, _chosenForm));
                return;
            }

            var form = arguments.Form;
            form.Judge();
            arguments.Why = form.Wrong;
            if (form.Wrong.Length > 0 || form.Missing)
            {
                form.Take(Editor.Input);
                return;
            }

            Give(form.Answer);
        }

        /// <summary>Finishes with the values <paramref name="said" /> gives, written as a line of values.</summary>
        public void Give(string said)
        {
            var arguments = new List<Literal>();
            ValueLine.Read(said, arguments);
            Named(arguments);

            // Toggle on a switch is the game's toggle for it, which takes nothing.
            if (_chosenForm == Form.Value && arguments.Count == 1 && arguments[0].Kind == ValueKind.Text
                && arguments[0].Text == EntityAction.Toggle)
            {
                var toggle = Vocabulary.Toggle(Editor.World, _selection, _chosen) ?? _chosen;
                _done(new PickAnswer(_selection, toggle, _all, new List<Literal>(), Form.Action));
                return;
            }

            _done(new PickAnswer(_selection, _chosen, _all, arguments, _chosenForm));
        }

        /// <summary>Gives the argument of each field for one of a list of names as that name.</summary>
        /// <remarks>The answer comes as a line of text, which carries a chosen word as a bare word, and a bare word reads as text.</remarks>
        void Named(List<Literal> arguments)
        {
            var fields = Fields;
            var at = 0;
            for (var f = 0; f < fields.Count && at < arguments.Count; f++)
            {
                if (fields[f].Entry == Entry.Name && arguments[at].Kind == ValueKind.Text)
                    arguments[at] = Literal.Name(arguments[at].Text);

                at += fields[f].Covers;
            }
        }

        /// <inheritdoc />
        public override void Build()
        {
            Collect();

            if (_aim != null)
                _aim.Pick = _strip;

            // Back from a page after this one, whatever that page asked goes with it; and back here from the page before,
            // what this one asked is put back.
            _strip.Return(_base, _base + Own);
            if (_strip.Stages.Count == _base && _blocks != null)
            {
                _strip.Add(_blocks);
                if (_what != null)
                    _strip.Add(_what);

                if (_does != null)
                    _strip.Add(_does);
            }

            _strip.Returns = _base > 0 ? Editor.Act(Cmd.PageBack, PageBack) : null;
            _strip.Onward = _aim != null && _aim.Outcome == Outcome.One ? Editor.Act(Cmd.PageOn, Primary) : null;

            if (_finish == null)
                _finish = Editor.Act(Cmd.Primary, Primary);

            if (_blocks == null)
                MakeBlocks();

            FillBlocks();



            // The second stage exists only once there is something for it to be about, and never where only blocks are asked.
            var wanted = _selection.Count > 0 && _fits == null;
            if (wanted && _what == null)
            {
                MakeWhat();
            }
            else if (!wanted && _what != null)
            {
                if (_does != null)
                {
                    _strip.RemoveLast();
                    _does = null;
                }

                _strip.RemoveLast();
                _what = null;
            }

            if (_what != null)
                FillWhat();

            // What the trigger does, beside what happens, once what happens is chosen.
            var does = _what != null && _aim != null && !string.IsNullOrEmpty(_chosen);
            if (does && _does == null)
            {
                _does = new StageColumn(_strip.Width, false, null, Editor.Texts);
                _does.Title = Editor.Texts.PickWhatItDoes;
                _strip.Add(_does);
            }
            else if (!does && _does != null)
            {
                _strip.RemoveLast();
                _does = null;
            }

            FillGiven();

            if (_does != null && _aim != null)
                _aim.Fill(_does);

            _strip.Show();
            _strip.Arrive(Opens, Editor.Input);

            if (!_goesOn)
                return;

            _goesOn = false;
            Primary();
        }

        /// <summary>Moves the strip's page turning on a tick.</summary>
        public override void Refresh()
        {
            _strip.Tick();
        }

        /// <summary>Goes straight on to the page after this one as soon as this is built, for a part pressed that stands for something on that page.</summary>
        /// <remarks>Built first all the same, because the page after carries on from the stages this one asks.</remarks>
        public void GoOn()
        {
            _goesOn = true;
        }

        /// <summary>Says whether this page is being opened straight onto, rather than paged to.</summary>
        public void Arrived(bool straight)
        {
            _straight = straight;
        }

        /// <summary>Gives the keyboard to the box of the column the pick opens on, now that there is a screen for it.</summary>
        protected override void ViewCreated()
        {
            _strip.Arrive(Opens, Editor.Input);
        }

        void MakeBlocks()
        {
            _blocks = new StageColumn(_strip.Width, true, null, Editor.Texts);
            _blocks.Title = Editor.Texts.PickBlocks;
            _blocks.EmptyText = _fits != null
                ? Editor.Texts.BlueprintNoneFits
                : _forTests ? Editor.Texts.PickNoBlocksChecked
                : _forEvents ? Editor.Texts.PickNoBlocksHappen : Editor.Texts.PickNoBlocksDone;

            // For the whole life of the column, so the line does not come and go as the list of kinds is narrowed.
            _blocks.HasKinds = _kinds.Count > 0;
            _blocks.Narrowed += OnBlockSearch;
            _blocks.KindsNarrowed += NarrowKinds;

            _blockRows = new Keyed<long, PickRow>(_blocks.Rows, BlockRow);
            _kindRows = new Keyed<string, KindRow>(_blocks.Kinds, KindRow);
            _blocks.KindsOpened += delegate
            {
                if (Editor.Input != null)
                    Editor.Input.Focus(_blocks.KindNarrow);
            };
            _clearKind = Editor.Act(Cmd.AnyKind, delegate { SetKind(""); });
            _toggleHidden = Editor.Act(Cmd.HiddenBlocks, delegate
            {
                ShowsHidden = !ShowsHidden;
                Editor.Invalidate();
            });
            _blocks.ShowHidden = _toggleHidden;

            _strip.Add(_blocks);
        }

        void MakeWhat()
        {
            // Two ways of looking at what somebody does to a block or checks of one. What happens to one is Sprocket's own
            // wording throughout, so there is no second way to look at it.
            // The angle marks point the way each link goes; the words alone are translated.
            var views = _forEvents ? null : new[] { "‹ " + Editor.Texts.PickToOrganized, Editor.Texts.PickToTerminal + " ›" };

            _what = new StageColumn(_strip.Width, true, views, Editor.Texts);
            _what.Title = _forTests ? Editor.Texts.PickWhatToCheck : _forEvents ? Editor.Texts.PickWhatHappens : Editor.Texts.PickWhatToDo;
            _what.EmptyText = _forTests
                ? Editor.Texts.PickNothingChecked
                : _forEvents ? Editor.Texts.PickNothingHappens : Editor.Texts.PickNothingDone;
            _what.Narrowed += OnWhatSearch;
            _whatRows = new Keyed<string, PickRow>(_what.Rows, WhatRow);

            _organized = Editor.Act(Cmd.ShowOrganized, delegate { Show(false); });
            _terminal = Editor.Act(Cmd.ShowTerminal, delegate { Show(true); });

            _strip.Add(_what);
        }

        PickRow BlockRow(long id)
        {
            var row = new PickRow();
            row.Picked += delegate(Held held, int presses) { Picked(id, held, presses); };
            row.Ticks = Editor.Act(Cmd.PickEntity, delegate { Pick(id, Held.Also); });
            return row;
        }

        KindRow KindRow(string kind)
        {
            var family = kind.StartsWith(FamilyPrefix, StringComparison.Ordinal)
                ? (Family)Enum.Parse(typeof(Family), kind.Substring(FamilyPrefix.Length))
                : Family.None;

            return new KindRow
            {
                Command = Editor.Act(kind.Length == 0 ? Cmd.AnyKind : Cmd.OneKind, delegate
                {
                    _blocks.CloseKinds();
                    if (family != Family.None)
                        SetFamily(family);
                    else
                        SetKind(kind);
                })
            };
        }

        PickRow WhatRow(string key)
        {
            var row = new PickRow();
            row.Picked += delegate(Held held, int presses) { Chosen(key, presses); };
            return row;
        }

        /// <summary>Takes a block the way the press said, and moves on where a plain press was a second in quick succession.</summary>
        /// <remarks>
        /// A plain press takes this one and nothing else, so pressing it twice is taking one thing and moving on, the same
        /// gesture as in a list where only one can be chosen. With something held, a second press is building a set, and
        /// moving on would cut that short.
        /// </remarks>
        void Picked(long id, Held held, int presses)
        {
            var pick = Editor.Act(Cmd.PickEntity, delegate { Pick(id, held); });
            if (pick == null)
                return;

            pick.Execute();
            if (presses > 1 && held == Held.Alone)
                MoveOn();
        }

        /// <summary>Chooses what the row under <paramref name="key" /> offers, and moves on where it was pressed twice in quick succession.</summary>
        void Chosen(string key, int presses)
        {
            int at;
            if (!_whatAt.TryGetValue(key, out at) || at < 0)
                return;

            var id = _ids[at];
            var form = _forms.Count > at ? _forms[at] : Form.Native;
            var choose = Editor.Act(Cmd.PickChoice, delegate { Choose(id, form); });
            if (choose == null)
                return;

            choose.Execute();
            if (presses > 1)
                MoveOn();
        }

        /// <summary>
        /// Finishes the way Done does, as one gesture with a choice: through the values where there are any, which go to the
        /// first that needs giving unless every one already holds something it takes.
        /// </summary>
        void MoveOn()
        {
            if (PrimaryText.Length == 0 || !CanPrimary)
                return;

            var arguments = _strip.Arguments;
            if (_strip.ShowsArguments && arguments != null && arguments.Form.Count > 0 && !arguments.Form.Complete)
            {
                arguments.Form.Take(Editor.Input);
                return;
            }

            if (_finish != null)
                _finish.Execute();
        }

        /// <summary>Pages back to the page before, which from then on this page was paged to.</summary>
        void PageBack()
        {
            _straight = false;
            Editor.Pop();
        }

        /// <summary>Leaves the pick from a page opened straight onto, past the page before that was never looked at, asking first where anything has changed.</summary>
        bool LeavingStraight()
        {
            if (!IsComplete || !Moved)
            {
                Editor.Pop();
                return true;
            }

            Editor.Ask(Editor.Texts.PickKeepQuestion);
            Editor.AnswerPrimary(Editor.Texts.PickKeepThem, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, delegate
            {
                Editor.Pop();
                Editor.Pop();
            });

            return false;
        }

        List<Field> Wanted()
        {
            return IsComplete ? Editor.Fields(_selection, _chosen, _chosenForm) : _nothing;
        }

        /// <summary>
        /// Says what the chosen thing takes, and what it opens holding, where either has changed; and puts the values beside
        /// the stages once something that can be finished is chosen.
        /// </summary>
        /// <remarks>
        /// The same thing can be asked for differently as the blocks change - a hinge picked beside a rotor turns without a
        /// direction - so the fields are compared as well as what they are for.
        /// </remarks>
        void FillGiven()
        {
            // A state is chosen whole, being in it or not, so only a value is given anything.
            if (_forEvents || _forTests && _chosenForm != Form.Value)
            {
                _strip.ShowsArguments = false;
                return;
            }

            var shown = IsComplete ? _chosenForm + " " + _chosen : "";
            _strip.ShowsArguments = _what != null && shown.Length != 0;

            var fields = Wanted();
            if (_shown == shown && Same(_shownFields, fields))
                return;

            _shown = shown;
            _shownFields.Clear();
            _shownFields.AddRange(fields);

            // Read once, where a color is asked for: these are the colors this player paints with, only ever read.
            var colors = new List<Hsv>();
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Edits != Features.Fields.Editor.Colour)
                    continue;

                Player.Colours(colors);
                break;
            }

            var made = _strip.Arguments == null;
            var arguments = _strip.Restate(shown, Chosen(), fields, Filled, colors);
            if (made)
            {
                arguments.Form.Edited += delegate
                {
                    if (Editor.Current != this)
                        return;

                    _entered = arguments.Form.Answer;
                    arguments.Why = arguments.Form.Wrong;
                };

                arguments.Form.Submitted += delegate
                {
                    if (Editor.Current == this && _finish != null)
                        _finish.Execute();
                };
            }

            _entered = arguments.Form.Answer;

            // Nowhere to type is nowhere to keep the keyboard. Somebody who came for the values gets it in them, once.
            if (_opens == Opening.Arguments && !_openedArguments && arguments.Form.Count > 0)
            {
                _openedArguments = true;
                arguments.Form.Take(Editor.Input);
            }
        }

        static bool Same(IList<Field> held, IList<Field> fields)
        {
            if (held.Count != fields.Count)
                return false;

            for (var i = 0; i < held.Count; i++)
            {
                if (held[i].Name != fields[i].Name || held[i].Least != fields[i].Least
                    || held[i].Most != fields[i].Most || held[i].Edits != fields[i].Edits
                    || held[i].Entry != fields[i].Entry || held[i].Covers != fields[i].Covers)
                {
                    return false;
                }
            }

            return true;
        }

        static bool Same(Literal a, Literal b)
        {
            var numbers = (a.Kind == ValueKind.Integer || a.Kind == ValueKind.Real)
                && (b.Kind == ValueKind.Integer || b.Kind == ValueKind.Real);

            return numbers ? a.Number == b.Number : a.Kind == b.Kind && a.Said == b.Said;
        }

        void FillBlocks()
        {
            var texts = Editor.Texts;
            _blocks.Subtitle = _selection.Count == 0
                ? texts.PickNonePicked
                : _selection.Count == 1
                    ? texts.PickOnePicked
                    : Template.Fill(texts.PickSomePicked, "count", _selection.Count.ToString(CultureInfo.InvariantCulture));

            // Always offered, never conditioned on how long the list is.
            _blocks.NarrowText = _screen.Search;

            // Off the whole catalogue, not the rows shown: narrowing must not take the picture column away and shuffle every
            // row sideways.
            var icons = false;
            for (var i = 0; i < _candidates.Count && !icons; i++)
                icons = !string.IsNullOrEmpty(_candidates[i].Kind.Icon);

            FillFilters();
            FillOptions();

            _blocks.Showing = _filtered.Count == _candidates.Count
                ? Template.Fill(texts.PickAllShown, "count", _candidates.Count.ToString(CultureInfo.InvariantCulture))
                : Template.Fill(Template.Fill(texts.PickSomeShown, "shown", _filtered.Count.ToString(CultureInfo.InvariantCulture)),
                    "count", _candidates.Count.ToString(CultureInfo.InvariantCulture));

            _blockRows.Show(_filteredIds);
            for (var i = 0; i < _filtered.Count; i++)
            {
                var entity = _filtered[i];
                var row = _blockRows[entity.Id];

                string note;
                row.Text = entity.Name;
                row.Secondary = _ahead.TryGetValue(entity.Id, out note) ? note : entity.Kind.DisplayName;
                row.Icon = entity.Kind.Icon;
                row.HasPicture = icons;
                row.Many = true;
                row.IsChosen = Contains(_selection, entity.Id);
                row.Show();
            }

            _blocks.ShowEmpty();
        }

        void FillFilters()
        {
            var texts = Editor.Texts;

            // A family left with one kind of block on the construct stands as that kind alone, and so does the narrowing to it.
            if (_family != Family.None && KindsOf(_family) == 1)
                SetKind(OnlyKindOf(_family).Id);

            // A kind whose last block has gone from the construct would hide every block with nothing on screen to say why.
            if (_kind.Length > 0 && !HasKind(_kind))
                SetKind("");

            _shownKinds.Clear();
            _shownFamilies.Clear();
            for (var i = 0; i < _kinds.Count; i++)
            {
                var family = _kinds[i].Family;
                if (family == Family.None || KindsOf(family) < 2)
                {
                    if (Offered(_kinds[i]))
                        Shows(_kinds[i], Family.None);

                    continue;
                }

                // A family is listed whole at its first kind, so its kinds stand together under it whatever their names.
                if (Listed(family, i))
                    continue;

                var named = EntityQuery.MatchesText(FamilyText.Of(texts, family), _kindSearch);
                var at = _shownKinds.Count;
                for (var j = i; j < _kinds.Count; j++)
                {
                    if (_kinds[j].Family == family && (named || Offered(_kinds[j])))
                        Shows(_kinds[j], family);
                }

                // The family in force is always offered, and a kind shown always has its family above it.
                if (named || _shownKinds.Count > at || _family == family)
                {
                    _shownKinds.Insert(at, null);
                    _shownFamilies.Insert(at, family);
                }
            }

            _kindKeys.Clear();
            _kindKeys.Add("");
            for (var i = 0; i < _shownKinds.Count; i++)
                _kindKeys.Add(_shownKinds[i] == null ? FamilyKey(_shownFamilies[i]) : _shownKinds[i].Id);

            _kindRows.Show(_kindKeys);

            var anything = _kindRows[""];
            anything.Text = texts.PickAnything;
            anything.Count = _candidates.Count;
            anything.IsCurrent = _kind.Length == 0;

            var current = texts.PickAnything;
            for (var i = 0; i < _shownKinds.Count; i++)
            {
                var row = _kindRows[_kindKeys[i + 1]];
                var kind = _shownKinds[i];
                row.Text = kind == null ? FamilyText.Of(texts, _shownFamilies[i]) : kind.DisplayName;
                row.Count = kind == null ? CountOfFamily(_shownFamilies[i]) : CountOfKind(kind.Id);
                row.IsNested = kind != null && _shownFamilies[i] != Family.None;
                row.IsCurrent = _kind == _kindKeys[i + 1];
                if (row.IsCurrent)
                    current = row.Text;
            }

            _blocks.Kind = current;
            _blocks.KindText = _kindSearch;
            _blocks.ClearKind = _kind.Length > 0 ? _clearKind : null;
            _blocks.ShowsHidden = ShowsHidden;
        }

        bool HasKind(string kindId)
        {
            for (var i = 0; i < _kinds.Count; i++)
            {
                if (_kinds[i].Id == kindId || (_family != Family.None && _kinds[i].Family == _family))
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="kind" /> is offered on its own: it is the one in force, or its name answers the box.</summary>
        /// <remarks>The one in force is always offered, so what the line says is showing is always something the list says too.</remarks>
        bool Offered(EntityKind kind)
        {
            return kind.Id == _kind || EntityQuery.MatchesText(kind.DisplayName, _kindSearch);
        }

        void Shows(EntityKind kind, Family family)
        {
            _shownKinds.Add(kind);
            _shownFamilies.Add(family);
        }

        /// <summary>Whether a kind of <paramref name="family" /> comes before the one at <paramref name="index" />, which listed the family already.</summary>
        bool Listed(Family family, int index)
        {
            for (var i = 0; i < index; i++)
            {
                if (_kinds[i].Family == family)
                    return true;
            }

            return false;
        }

        /// <summary>How many kinds of block on offer are of <paramref name="family" />.</summary>
        int KindsOf(Family family)
        {
            var count = 0;
            for (var i = 0; i < _kinds.Count; i++)
            {
                if (_kinds[i].Family == family)
                    count++;
            }

            return count;
        }

        EntityKind OnlyKindOf(Family family)
        {
            for (var i = 0; i < _kinds.Count; i++)
            {
                if (_kinds[i].Family == family)
                    return _kinds[i];
            }

            return null;
        }

        int CountOfFamily(Family family)
        {
            var count = 0;
            for (var i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].Kind.Family == family)
                    count++;
            }

            return count;
        }

        /// <summary>The key a family's row is kept under, made once for the page.</summary>
        string FamilyKey(Family family)
        {
            string key;
            if (!_familyKeys.TryGetValue(family, out key))
            {
                key = FamilyPrefix + family;
                _familyKeys.Add(family, key);
            }

            return key;
        }

        /// <summary>Narrows the kinds on offer, and nothing else.</summary>
        /// <remarks>
        /// The box over the kinds and the box over the blocks are two questions: one asks which sort, the other asks which one.
        /// Typing into either while the other holds something is an ordinary thing to do, so neither clears the other.
        /// </remarks>
        void NarrowKinds(string said)
        {
            said = said ?? "";
            if (said == _kindSearch)
                return;

            var narrow = Editor.Act(Cmd.NarrowKinds, delegate
            {
                _kindSearch = said;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>Offered wherever the distinction between any one block and all of them means anything: on a test.</summary>
        /// <remarks>
        /// A trigger that waits for all of its blocks is built and tested, but the choice would be offered on triggers it means
        /// nothing for, and nothing on the way in explains it. A trigger already saying "all of them" keeps saying it and
        /// firing that way.
        /// </remarks>
        void FillOptions()
        {
            var asks = _asksAll && (AsksAll || _forTests);
            if (!asks)
            {
                _blocks.Options.Clear();
                return;
            }

            if (_any == null)
            {
                _any = new OptionRow { Text = Editor.Texts.PickAnyOne, Command = Editor.Act(Cmd.RequireAny, delegate { SetAll(false); }) };
                _every = new OptionRow { Text = Editor.Texts.PickAllOfThem, Command = Editor.Act(Cmd.RequireAll, delegate { SetAll(true); }) };
            }

            if (_blocks.Options.Count == 0)
            {
                _blocks.Options.Add(_any);
                _blocks.Options.Add(_every);
            }

            _any.IsCurrent = !_all;
            _every.IsCurrent = _all;
        }

        void FillWhat()
        {
            _names.Clear();
            _ids.Clear();
            _forms.Clear();
            _groups.Clear();
            _marks.Clear();
            _sets.Clear();
            _ready.Clear();
            _why.Clear();

            if (_forTests)
            {
                Testable();
            }
            else if (_forEvents)
            {
                var events = Vocabulary.EventsOf(Editor.World, _selection);
                for (var i = 0; i < events.Count; i++)
                {
                    if (_signalsOnly && !events[i].Answered)
                        continue;

                    _names.Add(events[i].Text);
                    _marks.Add(MarkText.Of(events[i]));

                    _ids.Add(events[i].Id);
                    _sets.Add(false);
                }
            }
            else
            {
                Grouped(Vocabulary.ActionsOf(Editor.World, _selection));
            }

            var texts = Editor.Texts;
            var note = "";
            if (_forTests && Editor.ShowsTerminal)
            {
                note = texts.PickTerminalNote;
            }
            else if (!_forEvents && !_forTests)
            {
                note = Editor.ShowsTerminal ? texts.PickTerminalNote : PlanText.Note(texts, Vocabulary.Planned(Editor.World, _selection));
                if (Vocabulary.Narrowed(Editor.World, _selection, Editor.ShowsTerminal))
                    note = note.Length == 0
                        ? texts.PickShared
                        : Template.Fill(Template.Fill(texts.PickNoteAndShared, "shared", texts.PickShared), "note", note);
            }

            // A choice the blocks can no longer honor is dropped rather than left showing, so Done never appears over a
            // sentence that could not run. It is kept aside until something else is chosen, and blocks that honor it again
            // bring it back, so a detour through other blocks loses nothing: a step's own action comes back with its values.
            if (_chosen.Length > 0 && At(_chosen, _chosenForm) < 0)
            {
                _dropped = _chosen;
                _droppedForm = _chosenForm;
                _chosen = "";
                _chosenForm = Form.Native;
            }
            else if (_chosen.Length == 0 && _dropped.Length > 0 && At(_dropped, _droppedForm) >= 0)
            {
                _chosen = _dropped;
                _chosenForm = _droppedForm;
                _dropped = "";
            }

            var chosen = _chosen;

            _what.Note = note;
            _what.NarrowText = _whatSearch;
            _what.ShowViews(Editor.ShowsTerminal ? 1 : 0, new[] { _organized, _terminal });

            _matched.Clear();
            for (var i = 0; i < _names.Count; i++)
            {
                // What is chosen stays listed whatever the search, as a chosen block does, so a search never hides the
                // answer this picker opened with.
                if (EntityQuery.MatchesText(_names[i], _whatSearch) || _ids[i] == _chosen)
                    _matched.Add(i);
            }

            // A heading before the first of each group that survived the search, worked out here because which groups have
            // anything left in them is decided by what somebody has just typed. An event list has no groups.
            _rows.Clear();
            var group = -1;
            for (var i = 0; i < _matched.Count; i++)
            {
                var at = _matched[i];
                var mine = _forEvents ? group : _groups.Count > at ? _groups[at] : group;
                if (mine != group)
                {
                    group = mine;
                    _rows.Add(-1 - mine);
                }

                _rows.Add(at);
            }

            _whatKeys.Clear();
            _whatAt.Clear();
            for (var i = 0; i < _rows.Count; i++)
            {
                var at = _rows[i];
                var key = at < 0
                    ? "#" + (-1 - at).ToString(CultureInfo.InvariantCulture)
                    : (_forms.Count > at ? _forms[at] : Form.Native) + " " + _ids[at];

                _whatKeys.Add(key);
                _whatAt[key] = at;
            }

            _whatRows.Show(_whatKeys);

            for (var i = 0; i < _rows.Count; i++)
            {
                var at = _rows[i];
                var row = _whatRows[_whatKeys[i]];
                row.Many = false;
                row.HasPicture = false;
                row.Secondary = "";

                if (at < 0)
                {
                    row.Text = Heading(-1 - at);
                    row.IsHeading = true;
                    row.IsWithheld = false;
                    row.IsChosen = false;
                    row.Mark = "";
                    row.Status = "";
                    row.Why = null;
                    row.Show();
                    continue;
                }

                // Something that cannot be made into a step is listed to be read about and not chosen.
                var ready = _ready.Count > at ? _ready[at] : Readiness.Ready;
                var form = _forms.Count > at ? _forms[at] : Form.Native;

                row.Text = _names[at];
                row.IsHeading = false;
                row.IsWithheld = ready != Readiness.Ready;
                row.Why = row.IsWithheld ? _why[at] : null;
                row.Status = ReadinessText.Tag(Editor.Texts, ready);
                row.IsChosen = _ids[at] == chosen && form == _chosenForm;
                row.Mark = _marks.Count > at ? _marks[at] : "";
                row.About = MarkText.About(texts, row.Mark);
                row.Show();
            }

            _what.ShowEmpty();
        }

        void Show(bool terminal)
        {
            Editor.ShowsTerminal = terminal;
            Editor.Invalidate();
        }

        /// <summary>What a test can read of the blocks, in the view now showing.</summary>
        /// <remarks>
        /// The organized view lists each state the blocks can be in, and then not, and each value no state says, under the
        /// heading of the value it reads, as the actions are grouped. A state read the same as one already listed is left out:
        /// switched on and switched off are one state, read from either side. The terminal view lists the values the blocks
        /// declare, under the terminal's own names, in the order the game declared them.
        /// </remarks>
        void Testable()
        {
            var actions = Vocabulary.ActionsOf(Editor.World, _selection);
            if (Editor.ShowsTerminal)
            {
                actions.Sort(ByDeclared);
                for (var i = 0; i < actions.Count; i++)
                {
                    if (actions[i].Sets && actions[i].Unaltered)
                        Read(actions[i], Values);
                }

                return;
            }

            actions.Sort(BySaid);
            var events = Vocabulary.EventsOf(Editor.World, _selection);
            for (var group = 0; group <= (int)Category.Other; group++)
            {
                for (var i = 0; i < events.Count; i++)
                {
                    if (!events[i].IsState || _names.Contains(events[i].State) || (int)CategoryOf(actions, events[i].Value) != group)
                        continue;

                    Offer(events[i].State, events[i].Id, group);
                    Offer(events[i].Unstate, Not + events[i].Id, group);
                }

                for (var i = 0; i < actions.Count; i++)
                {
                    var one = actions[i];
                    var own = one.Being != null && !Stated(events, one.Id);
                    if ((own || (one.Sets && Lists(one))) && (int)one.Category == group)
                        Read(one, group);
                }
            }
        }

        /// <summary>Lists the value <paramref name="one" /> to be read, under <paramref name="group" />.</summary>
        void Read(EntityAction one, int group)
        {
            _names.Add(Said(one));
            _ids.Add(one.Id);
            _forms.Add(Form.Value);
            _groups.Add(group);
            _marks.Add(Symbols.Value);
            _sets.Add(true);
            _ready.Add(one.Readiness);
            _why.Add(ReadinessText.Why(Editor.Texts, one.Why));
        }

        /// <summary>The category of the value <paramref name="value" /> among <paramref name="actions" />, or the last where none is listed.</summary>
        static Category CategoryOf(List<EntityAction> actions, string value)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == value && (actions[i].Sets || actions[i].Being != null))
                    return actions[i].Category;
            }

            return Category.Other;
        }

        /// <summary>Whether a signal among <paramref name="events" /> says the value <paramref name="value" /> as a state.</summary>
        static bool Stated(List<EntityEvent> events, string value)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].IsState && events[i].Value == value)
                    return true;
            }

            return false;
        }

        /// <summary>Lists a state to be read, by the words it reads in and the choice it makes, under <paramref name="group" />.</summary>
        void Offer(string said, string id, int group)
        {
            _names.Add(said);
            _ids.Add(id);
            _forms.Add(Form.Native);
            _groups.Add(group);
            _marks.Add(Symbols.Value);
            _sets.Add(false);
            _ready.Add(Readiness.Ready);
            _why.Add(null);
        }

        /// <summary>Everything the blocks offer, values first and each group in its own order.</summary>
        /// <remarks>
        /// Sorted by the words rather than left in the order the game declared them, because a block hands its actions out in
        /// the order its code happens to create them and a person reading twenty of them is looking one up. The headings are
        /// not in here: a search matches only what somebody can choose.
        /// </remarks>
        void Grouped(List<EntityAction> actions)
        {
            actions.Sort(Editor.ShowsTerminal ? ByDeclared : BySaid);

            if (Editor.ShowsTerminal)
            {
                Listed(actions, Values);
                Listed(actions, Actions);
                return;
            }

            // Sprocket's groups, in the order the categories are declared, so a player who has learnt where a light's color
            // sits finds a sorter's in the same place.
            for (var group = 0; group <= (int)Category.Other; group++)
                Listed(actions, group);
        }

        void Listed(List<EntityAction> actions, int group)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                var one = actions[i];
                if (!Lists(one) || Group(one) != group)
                    continue;

                _names.Add(Said(one));
                _ids.Add(one.Id);
                _forms.Add(one.Form);
                _groups.Add(group);
                _marks.Add(MarkText.Of(one));
                _sets.Add(one.Sets);
                _ready.Add(one.Readiness);
                _why.Add(ReadinessText.Why(Editor.Texts, one.Why));
            }
        }

        /// <summary>What the view now showing calls this one: the block's own words in the terminal view, and Sprocket's in the organized one.</summary>
        /// <remarks>Values and actions are listed together in the organized view, so everything is said as something to do.</remarks>
        string Said(EntityAction one)
        {
            if (Editor.ShowsTerminal)
                return one.Declared;

            var said = one.Sets ? one.Text : one.Named;
            if (one.Unaltered && !one.Renamed)
                said = Unlabelled(said);

            return string.IsNullOrEmpty(said) ? said : char.ToUpperInvariant(said[0]) + said.Substring(1);
        }

        /// <summary>The game's name for a member, without the colon it ends a terminal label with.</summary>
        /// <remarks>Only ever given the game's own names: a word Sprocket or a mod's definition chose is left as written.</remarks>
        static string Unlabelled(string named)
        {
            if (string.IsNullOrEmpty(named))
                return named;

            var trimmed = named.TrimEnd();
            return trimmed.EndsWith(":", StringComparison.Ordinal)
                ? trimmed.Substring(0, trimmed.Length - 1).TrimEnd()
                : named;
        }

        /// <summary>Whether what is on offer under that id can be made into a step.</summary>
        Readiness ReadinessOf(string id, Form form)
        {
            var at = At(id, form);
            return at >= 0 && at < _ready.Count ? _ready[at] : Readiness.Ready;
        }

        /// <summary>Works the title out again where the answer has changed since it was last worked out.</summary>
        /// <remarks>Asked after every command and every tick, so it compares before it builds: a tick where nothing was picked makes no string.</remarks>
        void Reword()
        {
            // A slot's pick keeps the slot's name: the blocks chosen for it are the whole answer, and they are marked in its column.
            if (_fits != null)
                return;

            if (_said != null && _saidChosen == _chosen && _saidOffered == _ids.Count && _saidAll == _all
                && Same(_saidBlocks, _selection))
            {
                return;
            }

            _saidChosen = _chosen;
            _saidOffered = _ids.Count;
            _saidAll = _all;
            _saidBlocks.Clear();
            _saidBlocks.AddRange(_selection);

            var at = _chosen.Length == 0 ? -1 : At(_chosen, _chosenForm);
            var chosen = at < 0 ? "" : _names[at];

            // A step acts on every block it names, so its blocks are always joined with "and". Only what starts something can
            // be any one of them, and then only where it was asked.
            var blocks = SentenceText.Entities(Editor.World, _selection, !_forEvents && !_forTests || _all);

            _said = _selection.Count == 0 && chosen.Length == 0
                ? _title
                : _forTests
                    ? SentenceText.Testing(blocks, chosen, at >= 0 && _sets[at])
                    : SentenceText.Picking(_forEvents, blocks, chosen, at >= 0 && _sets[at]);

            _retitled = true;
        }

        static bool Same(List<long> one, List<long> other)
        {
            if (one.Count != other.Count)
                return false;

            for (var i = 0; i < one.Count; i++)
            {
                if (one[i] != other[i])
                    return false;
            }

            return true;
        }

        /// <summary>The name what is chosen is listed under, or empty where nothing is.</summary>
        string Chosen()
        {
            var at = At(_chosen, _chosenForm);
            return at < 0 ? "" : _names[at];
        }

        /// <summary>Where the thing on offer under that id is in the list, or -1 where it is not listed.</summary>
        /// <remarks>By the id and by which of the three ways it reaches the block, because a switch's value and the act that flips it share one id.</remarks>
        int At(string id, Form form)
        {
            for (var i = 0; i < _ids.Count; i++)
            {
                if (_ids[i] == id && (_forms.Count > i ? _forms[i] : Form.Native) == form)
                    return i;
            }

            return -1;
        }

        /// <summary>Whether the view now showing lists this one.</summary>
        /// <remarks>
        /// The terminal view lists what the block declares, whether or not something has been offered in its place; the
        /// organized view lists what is on offer and usable, whoever worded it. A value of
        /// Sprocket's own is a step only where it can be set; a test lists it itself.
        /// </remarks>
        bool Lists(EntityAction one)
        {
            if (one.Being != null && !one.Sets)
                return false;

            if (Editor.ShowsTerminal)
                return one.Unaltered;

            return one.Vetted && !one.Hidden && one.Readiness == Readiness.Ready;
        }

        /// <summary>Which group of the view now showing this one is listed in: the terminal's two kinds of member, or the organized view's categories.</summary>
        int Group(EntityAction one)
        {
            if (Editor.ShowsTerminal)
                return one.Sets ? Values : Actions;

            return (int)one.Category;
        }

        /// <summary>What a group is headed.</summary>
        string Heading(int group)
        {
            if (Editor.ShowsTerminal)
                return group == Values ? Editor.Texts.PickValues : Editor.Texts.PickToolbarActions;

            return CategoryText.Of(Editor.Texts, (Category)group);
        }

        int CountOfKind(string kindId)
        {
            var count = 0;
            for (var i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].Kind.Id == kindId)
                    count++;
            }

            return count;
        }

        /// <summary>Works out what is on offer, which does not depend on what has been picked.</summary>
        /// <remarks>
        /// Ticking a block narrows what can be said about it in the next stage and nothing else, so nobody watches the list of
        /// their own blocks shrink underneath them. Narrowing is the one thing that can hide a row, and a picked block is
        /// exempt: a tick nobody can find is a tick nobody can clear. A row stays on the list until the narrowing changes, so a
        /// block kept only because it was picked is still there after somebody picks another in its place. Sorted by name,
        /// because the order the world hands them over is whatever walking the grid produced.
        /// <para>
        /// A block hidden from toolbars is left out until the eye is opened, as the game's toolbar list leaves it out, unless
        /// it is picked.
        /// </para>
        /// </remarks>
        void Collect()
        {
            _candidates.Clear();

            if (_fits != null)
            {
                var entities = Editor.World.Entities;
                for (var i = 0; i < entities.Count; i++)
                {
                    if (_fits(entities[i]) && Listed(entities[i]))
                        _candidates.Add(entities[i]);
                }
            }
            else
            {
                var capable = _forTests ? Vocabulary.Testable(Editor.World) : Vocabulary.Capable(Editor.World, _forEvents);
                for (var i = 0; i < capable.Count; i++)
                {
                    if ((_admits == null || _admits(capable[i])) && Listed(capable[i]))
                        _candidates.Add(capable[i]);
                }
            }

            _candidates.Sort(ByName);
            Lead();

            _kinds.Clear();
            EntityQuery.KindsPresent(_candidates, _kinds);

            _wasShown.Clear();
            if (_screen.Search == _narrowedSearch && _kind == _narrowedKind)
            {
                for (var i = 0; i < _filteredIds.Count; i++)
                    _wasShown.Add(_filteredIds[i]);
            }

            _narrowedSearch = _screen.Search;
            _narrowedKind = _kind;

            _filtered.Clear();
            _filteredIds.Clear();
            for (var i = 0; i < _candidates.Count; i++)
            {
                var entity = _candidates[i];
                if (!_wasShown.Contains(entity.Id) && !Contains(_selection, entity.Id)
                    && !EntityQuery.Matches(entity, _screen.Search, _kind, _family))
                    continue;

                _filtered.Add(entity);
                _filteredIds.Add(entity.Id);
            }
        }

        /// <summary>Moves the blocks listed ahead of the rest to the front, keeping their order among themselves and the rest's among theirs.</summary>
        void Lead()
        {
            if (_ahead.Count == 0)
                return;

            var at = 0;
            for (var i = 0; i < _candidates.Count; i++)
            {
                if (!_ahead.ContainsKey(_candidates[i].Id))
                    continue;

                var entity = _candidates[i];
                _candidates.RemoveAt(i);
                _candidates.Insert(at++, entity);
            }
        }

        /// <summary>Whether <paramref name="entity" /> is listed: shown in toolbars, or the eye open, or picked already.</summary>
        bool Listed(GridEntity entity)
        {
            return ShowsHidden || entity.Block == null || entity.Block.ShowInToolbarConfig || Contains(_selection, entity.Id);
        }

        /// <summary>Takes a block, the way the press said it was taken.</summary>
        /// <remarks>
        /// Three acts and not three commands, because they are one act with a qualifier: a row was chosen, and the qualifier
        /// says what happens to everything else. The anchor a run starts from follows the presses that take one block
        /// deliberately, and shift with nothing to reach back to is the first press of a run.
        /// </remarks>
        void Pick(long entityId, Held how)
        {
            if (how == Held.Through && _anchor != 0L)
            {
                Span(_anchor, entityId);
                Editor.Invalidate();
                return;
            }

            _anchor = entityId;

            if (how == Held.Also)
                Toggle(entityId);
            else
                Only(entityId);

            Editor.Invalidate();
        }

        void Toggle(long entityId)
        {
            if (Contains(_selection, entityId))
                _selection.Remove(entityId);
            else
                _selection.Add(entityId);
        }

        /// <summary>This one and nothing else, which is what pressing a row plainly means.</summary>
        void Only(long entityId)
        {
            _selection.Clear();
            _selection.Add(entityId);
        }

        /// <summary>Everything between two blocks, in the order they are listed.</summary>
        /// <remarks>A run is what somebody drew down the screen, so a block filtered out since the anchor was set leaves the selection alone.</remarks>
        void Span(long from, long to)
        {
            var first = Listed(from);
            var last = Listed(to);

            if (first < 0 || last < 0)
                return;

            if (first > last)
            {
                var swap = first;
                first = last;
                last = swap;
            }

            _selection.Clear();
            for (var i = first; i <= last; i++)
                _selection.Add(_filtered[i].Id);
        }

        int Listed(long entityId)
        {
            for (var i = 0; i < _filtered.Count; i++)
            {
                if (_filtered[i].Id == entityId)
                    return i;
            }

            return -1;
        }

        void Choose(string id, Form form)
        {
            _chosen = id;
            _chosenForm = form;
            _dropped = "";
            Editor.Invalidate();
        }

        void SetAll(bool all)
        {
            _all = all;
            Editor.Invalidate();
        }

        /// <summary>Takes new search text, and ignores text that is not new.</summary>
        void OnBlockSearch(string value)
        {
            value = value ?? "";
            if (value == _screen.Search)
                return;

            var narrow = Editor.Act(Cmd.NarrowEntities, delegate
            {
                _screen.Search = value;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        void OnWhatSearch(string value)
        {
            value = value ?? "";
            if (value == _whatSearch)
                return;

            var narrow = Editor.Act(Cmd.NarrowChoices, delegate
            {
                _whatSearch = value;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>Narrows the blocks to one kind.</summary>
        void SetKind(string kindId)
        {
            _kind = kindId ?? "";
            _family = Family.None;
            Editor.Invalidate();
        }

        /// <summary>Narrows the blocks to every kind of <paramref name="family" />.</summary>
        void SetFamily(Family family)
        {
            _kind = FamilyKey(family);
            _family = family;
            Editor.Invalidate();
        }

        static bool Contains(IList<long> ids, long id)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }

        sealed class SaidOrder : IComparer<EntityAction>
        {
            readonly bool _declared;

            public SaidOrder(bool declared)
            {
                _declared = declared;
            }

            public int Compare(EntityAction left, EntityAction right)
            {
                return string.Compare(Said(left), Said(right), StringComparison.OrdinalIgnoreCase);
            }

            string Said(EntityAction one)
            {
                if (one == null)
                    return "";

                if (_declared)
                    return one.Declared;

                return one.Sets ? one.Text : one.Named;
            }
        }
    }
}
