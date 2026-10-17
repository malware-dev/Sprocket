using Sprocket.Host.SpaceEngineers.Features.Codex;
using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Storage;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Writing;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>Picks blocks and then what happens to them, with both stages on screen at once.</summary>
    /// <remarks>
    /// Side by side, the whole path reads at a glance and changing an earlier answer is a glance to the left. The page keeps
    /// its own answer and hands it back when somebody says they are done: ticking entries straight into a step would make a
    /// tentative tap an edit to a sequence that might be running, and would leave back with nothing to back out of. The
    /// strip it is drawn in is declared in <c>PickPage.View.cs</c>, and shared with the page before or after it in the same
    /// pick.
    /// </remarks>
    public sealed partial class PickPage : EditorPage
    {

        readonly string _title;
        readonly List<long> _selection = new List<long>();
        readonly bool _forEvents;
        readonly bool _forTests;

        // Whether the values last opened show what the block holds now rather than what anybody gave.
        bool _offering;
        readonly bool _asksAll;
        readonly Action<PickAnswer> _done;
        readonly EditorPage _screen;
        readonly List<int> _matched = new List<int>();
        readonly List<int> _rows = new List<int>();
        readonly List<string> _whatKeys = new List<string>();
        readonly List<string> _whatShown = new List<string>();
        readonly Dictionary<string, int> _whatAt = new Dictionary<string, int>();
        readonly List<long> _arrived = new List<long>();
        readonly List<Literal> _arrivedValues = new List<Literal>();
        readonly Opening _opens;
        readonly Aim _aim;
        readonly int _base;
        readonly string _arrivedChosen;
        readonly ToolbarSlot _arrivedSlot;
        readonly string _arrivedFacet;
        readonly Nature _arrivedForm;
        readonly Quantifier _arrivedQuantifier;
        readonly List<string> _filled = new List<string>();
        readonly List<Field> _nothing = new List<Field>();
        readonly List<Field> _shownFields = new List<Field>();
        readonly Func<GridEntity, bool> _fits;
        readonly bool _signalsOnly;
        readonly bool _memberOnly;
        readonly Offers _offers;
        readonly Occupancy _occupancy;
        readonly Wording _wording;
        readonly Listing _listing;
        readonly Kinds _kinds = new Kinds();

        string _chosen;
        ToolbarSlot _slot;
        string _facet;
        Nature _chosenForm;
        string _dropped = "";
        Nature _droppedForm;
        Quantifier _quantifier;
        string _whatSearch = "";
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
        Keyed<string, Element> _whatRows;
        SlotStrip _slots;
        FacetChoice _facets;
        ListField _reads;
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
        /// <paramref name="forTests" /> offers what an if can test: the values of its blocks, each compared as its options say.
        /// <paramref name="facet" /> is the name of the facet the step acts on, where what it does is a member of a type the host
        /// provides. Opened the way the last picker of its sort was left: what is on this construct is not known yet, so a kind it does
        /// not have is dropped later, where it is.
        /// </remarks>
        public PickPage(SprocketEditor editor, string title, bool forEvents,
            IList<long> targets, string chosen, Nature chosenForm, Action<PickAnswer> done,
            bool asksAll = false, Quantifier quantifier = Quantifier.Any, IList<Literal> arguments = null,
            Opening opens = Opening.Blocks, Aim aim = null, PickStrip after = null, Func<GridEntity, bool> admits = null,
            bool signalsOnly = false, bool forTests = false, string facet = "")
            : base(editor)
        {
            _facet = facet ?? "";
            _arrivedFacet = _facet;
            _forTests = forTests;
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
            _arrivedQuantifier = quantifier;
            _asksAll = asksAll;
            _quantifier = quantifier;
            _done = done;

            _screen = editor.Opener;

            var pressed = forEvents ? Occupancy.Pressing(editor.World, targets, _chosen) : null;
            if (pressed != null)
            {
                _slot = ToolbarSlot.Of(_chosen);
                _chosen = pressed.Id;
                _arrivedChosen = _chosen;
                _arrivedSlot = _slot;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                _selection.Add(targets[i]);
                _arrived.Add(targets[i]);
            }

            // A step that flips a switch was made by choosing toggle on the switch's value, so that is what it opens on.
            var flipped = !forEvents && !forTests && chosenForm == Nature.Action
                ? Vocabulary.Flipped(editor.World, _selection, _chosen)
                : null;
            if (flipped != null)
            {
                _chosen = flipped;
                _arrivedChosen = flipped;
                _chosenForm = Nature.Value;
                _arrivedForm = Nature.Value;
                _arrivedValues.Clear();
                _arrivedValues.Add(Literal.Of(EntityAction.Toggle));
            }

            _offers = new Offers(editor, forEvents, forTests, signalsOnly);
            _occupancy = new Occupancy(editor);
            _wording = new Wording(editor, _offers, title, forEvents, forTests);
            _listing = new Listing(editor, admits, forEvents, forTests);
        }

        /// <summary>Asks which blocks fill a slot: the blocks alone, only those <paramref name="fits" /> allows, starting from <paramref name="targets" />.</summary>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, Func<GridEntity, bool> fits,
            Action<PickAnswer> done)
            : this(editor, title, false, targets, "", Nature.Native, done)
        {
            _fits = fits;
        }

        /// <summary>
        /// Asks which blocks fill a slot of a type the host provides, and which of their facets: the member
        /// <paramref name="member" /> the slot's step does, reached as <paramref name="form" /> says, offered alone, asking
        /// for none of its values, starting from <paramref name="targets" /> and the facet called <paramref name="facet" />,
        /// and only blocks <paramref name="admits" /> allows listed.
        /// </summary>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, string member, Nature form, string facet,
            Func<GridEntity, bool> admits, Action<PickAnswer> done)
            : this(editor, title, false, targets, member, form, done, admits: admits, facet: facet)
        {
            _memberOnly = true;
        }

        /// <summary>The title as the answer stands, or the picker's own name where nothing is chosen.</summary>
        public override string Title => _wording.Said ?? _title;

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
        public IDictionary<long, string> Ahead => _listing.Ahead;

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
                _offering = false;
                if (_arrivedValues.Count > 0 && _chosen == _arrivedChosen && _chosenForm == _arrivedForm)
                {
                    Channels.Shown(_arrivedValues, Wanted(), _filled);
                    return _filled;
                }

                // A value being set opens on what the block holds now, as the terminal's own control does.
                if (_chosenForm == Nature.Value && !_forTests && !_forEvents && _selection.Count > 0)
                {
                    var current = Vocabulary.Current(Editor.World, _selection, _chosen);
                    if (current.HasValue)
                    {
                        Channels.Shown(new[] { Opaque(current.Value) }, Wanted(), _filled);
                        _offering = true;
                    }
                }

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

                foreach (var id in _selection)
                {
                    if (!Listing.Contains(_arrived, id))
                        return true;
                }

                if (_chosen != _arrivedChosen || _chosenForm != _arrivedForm || _quantifier != _arrivedQuantifier
                    || !ToolbarSlot.Same(_slot, _arrivedSlot) || _facet != _arrivedFacet)
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
            get { return Editor.Preferences?.ShowsHidden ?? _showsHidden; }
            set
            {
                if (Editor.Preferences != null)
                    Editor.Preferences.ShowsHidden = value;
                else
                    _showsHidden = value;
            }
        }

        bool IsComplete => _selection.Count > 0 && (_fits != null || !string.IsNullOrEmpty(_chosen)
            && _offers.ReadinessOf(_chosen, _chosenForm) == Readiness.Ready
            && (_aim == null || _aim.Chosen));

        /// <summary>The stages this page asks, which are the last in the pick while it is on top.</summary>
        int Own => (_blocks == null ? 0 : 1) + (_what == null ? 0 : 1) + (_does == null ? 0 : 1);

        /// <summary>The stage whose box takes the keyboard when its page is first shown, or minus one where none does.</summary>
        int Opens => _opens == Opening.Blocks ? _base : _opens == Opening.What ? _base + 1 : -1;

        /// <inheritdoc />
        public override bool TakeRetitle()
        {
            // A slot's pick keeps the slot's name: the blocks chosen for it are the whole answer, and they are marked in its column.
            if (_fits == null)
                _wording.Reword(_chosen, _chosenForm, _slot, _quantifier, Totals, _selection);

            return _wording.Take();
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

            var after = _aim?.Following;
            if (!IsComplete || !Moved && (after == null || !after.Moved))
                return true;

            Editor.Ask(Editor.Texts.PickKeepQuestion);
            Editor.AnswerPrimary(Editor.Texts.PickKeepThem, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, () => Editor.Pop());

            return false;
        }

        /// <summary>Finishes, with the values given in the column beside the stages where what is chosen takes any.</summary>
        /// <remarks>Every value is held to what it takes from here on, and where one fails the column says why and the keyboard goes to it.</remarks>
        public override void Primary()
        {
            var arguments = _strip.Arguments;
            if (_forEvents && _chosenForm != Nature.Value || !_strip.ShowsArguments || arguments == null || arguments.Form.Count == 0)
            {
                _done(new PickAnswer(_selection, Answered, _quantifier, null, _chosenForm, _facet));
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
            if (_chosenForm == Nature.Value && arguments.Count == 1 && arguments[0].Kind == ValueKind.Text
                && arguments[0].Text == EntityAction.Toggle)
            {
                var toggle = Vocabulary.Toggle(Editor.World, _selection, _chosen) ?? _chosen;
                _done(new PickAnswer(_selection, toggle, _quantifier, new List<Literal>(), Nature.Action, _facet));
                return;
            }

            _done(new PickAnswer(_selection, _chosen, _quantifier, arguments, _chosenForm, _facet));
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
            _listing.Collect(_selection, _screen.Search, _kinds.Kind, _kinds.Family, ShowsHidden, _fits, _kinds.Present);

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
            if (_slots != null)
                _slots.Slide();

            if (_facets != null)
                _facets.Slide();
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
            _blocks.HasKinds = _kinds.Present.Count > 0;
            _blocks.Narrowed += OnBlockSearch;
            _blocks.KindsNarrowed += NarrowKinds;

            _blockRows = new Keyed<long, PickRow>(_blocks.Rows, BlockRow);
            _kindRows = new Keyed<string, KindRow>(_blocks.Kinds, KindRow);
            _blocks.KindsOpened += delegate
            {
                if (Editor.Input != null)
                    Editor.Input.Focus(_blocks.KindNarrow);
            };
            _clearKind = Editor.Act(Cmd.AnyKind, () => SetKind(""));
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
            var views = _forEvents ? null : new[] { $"‹ {Editor.Texts.PickToOrganized}", $"{Editor.Texts.PickToTerminal} ›" };

            _what = new StageColumn(_strip.Width, true, views, Editor.Texts);
            _what.Title = _forTests ? Editor.Texts.PickWhatToCheck : _forEvents ? Editor.Texts.PickWhatHappens : Editor.Texts.PickWhatToDo;
            _what.EmptyText = _forTests
                ? Editor.Texts.PickNothingChecked
                : _forEvents ? Editor.Texts.PickNothingHappens : Editor.Texts.PickNothingDone;
            _what.Narrowed += OnWhatSearch;
            _whatRows = new Keyed<string, Element>(_what.Rows, WhatRow);

            _organized = Editor.Act(Cmd.ShowOrganized, () => Show(false));
            _terminal = Editor.Act(Cmd.ShowTerminal, () => Show(true));

            _strip.Add(_what);
        }

        PickRow BlockRow(long id)
        {
            var row = new PickRow();
            row.Picked += (held, presses) => Picked(id, held, presses);
            row.Ticks = Editor.Act(Cmd.PickEntity, () => Pick(id, Held.Also));
            return row;
        }

        KindRow KindRow(string kind)
        {
            var family = Kinds.FamilyOf(kind);

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

        Element WhatRow(string key)
        {
            if (key == SlotsKey)
            {
                _slots = new SlotStrip(Editor.Texts);
                _slots.Picked += PickSlot;
                return _slots;
            }

            if (key == FacetsKey)
            {
                _facets = new FacetChoice();
                _facets.Chosen += PickFacet;
                return _facets;
            }

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
            var pick = Editor.Act(Cmd.PickEntity, () => Pick(id, held));
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

            var id = _offers.Ids[at];
            var form = _offers.Forms.Count > at ? _offers.Forms[at] : Nature.Native;
            var choose = Editor.Act(Cmd.PickChoice, () => Choose(id, form));
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
            if (!IsComplete)
                return _nothing;

            // A number watched to start something is compared as a check compares it.
            if (_forEvents && _chosenForm == Nature.Value)
                return Comparing.Fields(Valued(Vocabulary.ActionsOf(Editor.World, _selection), _chosen), Vocabulary.EventsOf(Editor.World, _selection));

            if (!_forTests)
                return Editor.Fields(_selection, _chosen, _chosenForm);

            string value, state;
            return Comparing.Split(_chosen, out value, out state)
                ? Comparing.StateFields()
                : Comparing.Fields(Valued(Vocabulary.ActionsOf(Editor.World, _selection), _chosen), Vocabulary.EventsOf(Editor.World, _selection));
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
            // A moment takes nothing; a number watched asks how it compares, and with what, over what the trigger does, which
            // is the order the sentence says them in and keeps every column on the one page.
            if (_forEvents && _chosenForm != Nature.Value || _memberOnly)
            {
                _strip.ShowsArguments = false;
                return;
            }

            _strip.ArgumentsIn = _forEvents ? _does ?? _what : null;

            var shown = IsComplete ? $"{_chosenForm} {_chosen}" : "";
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
            if (_offering)
                arguments.Form.Offer();
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

                arguments.Form.Opens += lines =>
                {
                    if (Editor.Current == this)
                        Editor.Push(new TextPage(Editor, Title, lines.Text, lines.Longest, lines.Give));
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

        /// <summary>The value <paramref name="id" /> among <paramref name="actions" />, or null where none of them is.</summary>
        static EntityAction Valued(List<EntityAction> actions, string id)
        {
            foreach (var action in actions)
            {
                if (action.Id == id && (action.Sets || action.Being != null))
                    return action;
            }

            return null;
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
            for (var i = 0; i < _listing.Candidates.Count && !icons; i++)
                icons = !string.IsNullOrEmpty(_listing.Candidates[i].Kind.Icon);

            FillFilters();
            FillOptions();

            _blocks.Showing = _listing.Filtered.Count == _listing.Candidates.Count
                ? Template.Fill(texts.PickAllShown, "count", _listing.Candidates.Count.ToString(CultureInfo.InvariantCulture))
                : Template.Fill(Template.Fill(texts.PickSomeShown, "shown", _listing.Filtered.Count.ToString(CultureInfo.InvariantCulture)),
                    "count", _listing.Candidates.Count.ToString(CultureInfo.InvariantCulture));

            _blockRows.Show(_listing.FilteredIds);
            foreach (var entity in _listing.Filtered)
            {
                var row = _blockRows[entity.Id];

                string note;
                row.Text = entity.Name;
                row.Secondary = _listing.Ahead.TryGetValue(entity.Id, out note) ? note : entity.Kind.DisplayName;
                row.Icon = entity.Kind.Icon;
                row.HasPicture = icons;
                row.Many = true;
                row.IsChosen = Listing.Contains(_selection, entity.Id);
                row.Show();
            }

            _blocks.ShowEmpty();
        }

        void FillFilters()
        {
            var texts = Editor.Texts;

            if (_kinds.Settle())
                Editor.Invalidate();

            _kinds.Fill(texts);

            _kindRows.Show(_kinds.Keys);

            var anything = _kindRows[""];
            anything.Text = texts.PickAnything;
            anything.Count = _listing.Candidates.Count;
            anything.IsCurrent = _kinds.Kind.Length == 0;

            var current = texts.PickAnything;
            for (var i = 0; i < _kinds.Shown.Count; i++)
            {
                var row = _kindRows[_kinds.Keys[i + 1]];
                var kind = _kinds.Shown[i];
                row.Text = kind == null ? FamilyText.Of(texts, _kinds.Families[i]) : kind.DisplayName;
                row.Count = kind == null ? _listing.CountOfFamily(_kinds.Families[i]) : _listing.CountOfKind(kind.Id);
                row.IsNested = kind != null && _kinds.Families[i] != Family.None;
                row.IsCurrent = _kinds.Kind == _kinds.Keys[i + 1];
                if (row.IsCurrent)
                    current = row.Text;
            }

            _blocks.Kind = current;
            _blocks.KindText = _kinds.Search;
            _blocks.ClearKind = _kinds.Kind.Length > 0 ? _clearKind : null;
            _blocks.ShowsHidden = ShowsHidden;
        }

        /// <summary>Narrows the kinds on offer, and nothing else.</summary>
        /// <remarks>
        /// The box over the kinds and the box over the blocks are two questions: one asks which sort, the other asks which one.
        /// Typing into either while the other holds something is an ordinary thing to do, so neither clears the other.
        /// </remarks>
        void NarrowKinds(string said)
        {
            said = said ?? "";
            if (said == _kinds.Search)
                return;

            var narrow = Editor.Act(Cmd.NarrowKinds, delegate
            {
                _kinds.Search = said;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>
        /// Puts the choice of how several blocks are read under them, wherever the distinction between them means anything: on a
        /// test, and on a trigger watching for a state, never on a signal the blocks send. The ways are listed in the words the
        /// sentence says them in; a test is offered their sum and their average as well, which list only what is a number to check.
        /// </summary>
        void FillOptions()
        {
            var asks = _asksAll && (_forTests || _forEvents && !_signalsOnly && (_chosen.Length == 0 || Watched() || _chosenForm == Nature.Value || Totals));
            if (!asks)
            {
                _blocks.Options.Clear();
                return;
            }

            if (_reads == null)
            {
                var words = new List<string>();
                for (var i = 0; i < SentenceText.Readings.Count; i++)
                    words.Add(SentenceText.Quantified(SentenceText.Readings[i]));

                _reads = new ListField(words);
                _reads.Edited += delegate
                {
                    var reads = Editor.Act(Cmd.ReadBlocks, () => Quantify(Read(_reads.Text)));
                    if (reads != null)
                        reads.Execute();
                };
            }

            if (_blocks.Options.Count == 0)
                _blocks.Options.Add(_reads);

            var said = SentenceText.Quantified(_quantifier);
            if (_reads.Text != said)
                _reads.Text = said;
        }

        /// <summary>The way of reading the blocks <paramref name="said" /> names, as <see cref="SentenceText.Quantified" /> says them.</summary>
        static Quantifier Read(string said)
        {
            for (var i = 0; i < SentenceText.Readings.Count; i++)
            {
                if (SentenceText.Quantified(SentenceText.Readings[i]) == said)
                    return SentenceText.Readings[i];
            }

            return Quantifier.Any;
        }

        /// <summary>Whether the blocks are read by a total of a number on them, a sum or an average, which lists only numbers.</summary>
        bool Totals => _quantifier == Quantifier.Sum || _quantifier == Quantifier.Average;

        /// <summary>Whether what is chosen to start something is a state Sprocket watches the blocks for, rather than a signal they send.</summary>
        bool Watched()
        {
            if (!_forEvents || _chosen.Length == 0)
                return false;

            var events = Vocabulary.EventsOf(Editor.World, _selection);
            foreach (var entityEvent in events)
            {
                if (entityEvent.Id == _chosen)
                    return !entityEvent.Answered && entityEvent.Value.Length > 0;
            }

            return false;
        }

        void FillWhat()
        {
            _offers.Fill(_selection, Totals, _memberOnly ? _arrivedChosen : null);

            var texts = Editor.Texts;
            var note = "";
            if (_forTests || _forEvents)
            {
                note = _forTests && Editor.ShowsTerminal ? texts.PickTerminalNote : "";
                var totals = _quantifier == Quantifier.Sum ? texts.PickSumNote : _quantifier == Quantifier.Average ? texts.PickAverageNote : "";
                if (totals.Length > 0)
                    note = note.Length == 0 ? totals : $"{note} {totals}";
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
            if (_chosen.Length > 0 && _offers.At(_chosen, _chosenForm) < 0)
            {
                _dropped = _chosen;
                _droppedForm = _chosenForm;
                _chosen = "";
                _chosenForm = Nature.Native;
            }
            else if (_chosen.Length == 0 && _dropped.Length > 0 && _offers.At(_dropped, _droppedForm) >= 0)
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
            for (var i = 0; i < _offers.Names.Count; i++)
            {
                // What is chosen stays listed whatever the search, as a chosen block does, so a search never hides the
                // answer this picker opened with.
                if (EntityQuery.MatchesText(_offers.Names[i], _whatSearch) || _offers.Ids[i] == _chosen)
                    _matched.Add(i);
            }

            // A heading before the first of each group that survived the search, worked out here because which groups have
            // anything left in them is decided by what somebody has just typed. An event list has no groups.
            _rows.Clear();
            var group = -1;
            foreach (var at in _matched)
            {
                var mine = _offers.Groups.Count > at ? _offers.Groups[at] : group;
                if (mine != group)
                {
                    group = mine;
                    _rows.Add(-1 - mine);
                }

                _rows.Add(at);
            }

            _whatKeys.Clear();
            _whatAt.Clear();
            foreach (var at in _rows)
            {
                var key = at < 0
                    ? $"#{(-1 - at).ToString(CultureInfo.InvariantCulture)}"
                    : $"{(_offers.Forms.Count > at ? _offers.Forms[at] : Nature.Native)} {_offers.Ids[at]}";

                _whatKeys.Add(key);
                _whatAt[key] = at;
            }

            // The slots of a toolbar a person presses open under its line once it is chosen.
            _whatShown.Clear();
            _whatShown.AddRange(_whatKeys);
            var pressed = Occupancy.Pressing(Editor.World, _selection, _chosen);
            var under = pressed == null ? -1 : _whatKeys.IndexOf($"{Nature.Native} {pressed.Id}");
            if (under >= 0)
                _whatShown.Insert(under + 1, SlotsKey);

            // As do the facets a member of a type the host provides acts on, where the blocks share more than one.
            var type = pressed == null && !_forEvents ? Vocabulary.TypeOf(Editor.World, _selection, _chosen) : null;
            var shared = type == null ? null : Facets.Shared(Editor.World, _selection, type);
            Keep(shared);
            var over = shared == null || shared.Count < 2 ? -1 : _whatKeys.IndexOf($"{_chosenForm} {_chosen}");
            if (over >= 0)
                _whatShown.Insert(over + 1, FacetsKey);

            _whatRows.Show(_whatShown);
            if (under >= 0)
                ShowSlots(pressed);
            else
                _slots = null;

            if (over >= 0)
                _facets.Show(shared, _facet, Facets.Showing(Editor.World, _selection, type, _facet));
            else
                _facets = null;

            for (var i = 0; i < _rows.Count; i++)
            {
                var at = _rows[i];
                var row = (PickRow)_whatRows[_whatKeys[i]];
                row.Many = false;
                row.HasPicture = false;
                row.Secondary = "";

                if (at < 0)
                {
                    row.Text = _offers.Heading(-1 - at);
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
                var ready = _offers.Ready.Count > at ? _offers.Ready[at] : Readiness.Ready;
                var form = _offers.Forms.Count > at ? _offers.Forms[at] : Nature.Native;

                row.Text = _offers.Names[at];
                row.IsHeading = false;
                row.IsWithheld = ready != Readiness.Ready;
                row.Why = row.IsWithheld ? _offers.Why[at] : null;
                row.Status = ReadinessText.Tag(Editor.Texts, ready);
                row.IsChosen = _offers.Ids[at] == chosen && form == _chosenForm;
                row.Mark = _offers.Marks.Count > at ? _offers.Marks[at] : "";
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

        /// <summary>The name what is chosen is listed under, or empty where nothing is.</summary>
        string Chosen()
        {
            var at = _offers.At(_chosen, _chosenForm);
            return at < 0 ? "" : _offers.Names[at];
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
            if (Listing.Contains(_selection, entityId))
                _selection.Remove(entityId);
            else
                _selection.Add(entityId);
        }

        /// <summary>Takes the block <paramref name="entityId" /> and nothing else, which is what pressing a row plainly means.</summary>
        void Only(long entityId)
        {
            _selection.Clear();
            _selection.Add(entityId);
        }

        /// <summary>Takes every block listed from <paramref name="from" /> to <paramref name="to" />, in the order they are listed.</summary>
        /// <remarks>A run is what somebody drew down the screen, so a block filtered out since the anchor was set leaves the selection alone.</remarks>
        void Span(long from, long to)
        {
            var first = _listing.Index(from);
            var last = _listing.Index(to);

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
                _selection.Add(_listing.Filtered[i].Id);
        }

        /// <summary>The key the slots of a chosen toolbar are kept under among the rows, which no row's key can be.</summary>
        const string SlotsKey = "slots";

        /// <summary>The key the facets of a chosen member are kept under among the rows, which no row's key can be.</summary>
        const string FacetsKey = "facets";

        /// <summary>What is chosen as the program names it: the one slot, where a toolbar a person presses is chosen.</summary>
        string Answered => _slot == null ? _chosen : _slot.EventId;

        void ShowSlots(EntityEvent pressed)
        {
            _occupancy.Occupy(_selection, pressed);
            if (_slot == null)
                _slot = _occupancy.Free(_selection, pressed);

            _slots.Show(pressed.Pages, pressed.Slots, _occupancy.Held, _slot.Page, _slot.Slot);
        }

        /// <summary>
        /// Chooses the toolbar a person presses that has a slot read as <paramref name="words" />, "button 2 is pressed", and
        /// then that slot, as pressing the toolbar's line and then the slot under it does; false where no slot reads so.
        /// </summary>
        public bool ChooseSlot(string words)
        {
            if (!_forEvents)
                return false;

            foreach (var pressed in Vocabulary.EventsOf(Editor.World, _selection))
            {
                if (!pressed.IsPressed)
                    continue;

                var toolbar = ToolbarSlot.Of(pressed.Id).Toolbar;
                for (var i = 0; i < pressed.Pages * pressed.Slots; i++)
                {
                    var at = new ToolbarSlot(toolbar, i / pressed.Slots + 1, i % pressed.Slots + 1);
                    if (pressed.At(at).Text != words)
                        continue;

                    Chosen($"{Nature.Native} {pressed.Id}", 1);
                    PickSlot(at.Page, at.Slot);
                    return true;
                }
            }

            return false;
        }

        void PickSlot(int page, int slot)
        {
            var whole = ToolbarSlot.Of(_chosen);
            if (whole == null)
                return;

            var pick = Editor.Act(Cmd.PickChoice, delegate
            {
                _slot = new ToolbarSlot(whole.Toolbar, page, slot);
                Editor.Invalidate();
            });
            if (pick != null)
                pick.Execute();
        }

        /// <summary>Keeps the facet chosen where the blocks still share it, and otherwise chooses the first they share, or none.</summary>
        void Keep(List<string> shared)
        {
            if (shared == null || shared.Count == 0)
                _facet = "";
            else if (!shared.Contains(_facet))
                _facet = shared[0];
        }

        void PickFacet(string name)
        {
            var pick = Editor.Act(Cmd.PickChoice, delegate
            {
                _facet = name;
                Editor.Invalidate();
            });
            if (pick != null)
                pick.Execute();
        }

        void Choose(string id, Nature form)
        {
            var pressed = form == Nature.Native ? Occupancy.Pressing(Editor.World, _selection, id) : null;
            if (pressed == null)
                _slot = null;
            else if (id != _chosen || _slot == null)
                _slot = _occupancy.Free(_selection, pressed);

            _chosen = id;
            _chosenForm = form;
            _dropped = "";
            Editor.Invalidate();
        }

        /// <summary>Reads the blocks as <paramref name="quantifier" /> says, letting go of what is chosen where a sum or average cannot be taken of it.</summary>
        void Quantify(Quantifier quantifier)
        {
            _quantifier = quantifier;

            string value, state;
            if (Totals && _chosen.Length > 0
                && (Comparing.Split(_chosen, out value, out state) || !Offers.Number(Valued(Vocabulary.ActionsOf(Editor.World, _selection), _chosen))))
            {
                _chosen = "";
            }

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
            _kinds.Set(kindId);
            Editor.Invalidate();
        }

        /// <summary>Narrows the blocks to every kind of <paramref name="family" />.</summary>
        void SetFamily(Family family)
        {
            _kinds.SetFamily(family);
            Editor.Invalidate();
        }

        /// <summary><paramref name="value" /> as a step would give it: a color fully opaque, whatever the block reads back.</summary>
        static Literal Opaque(Literal value)
        {
            return value.Kind == ValueKind.Color ? Literal.Colour(value.Red, value.Green, value.Blue, 255) : value;
        }
    }
}
