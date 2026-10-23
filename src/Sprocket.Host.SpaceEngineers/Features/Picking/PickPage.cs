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
    /// The page keeps its own answer and hands it back only when the pick is done.
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
        readonly OpeningColumn _opens;
        readonly OutcomeColumn _aim;
        readonly int _base;
        readonly string _arrivedChosen;
        readonly ToolbarSlot _arrivedSlot;
        readonly string _arrivedFacet;
        readonly ReachKind _arrivedForm;
        readonly Quantifier _arrivedQuantifier;
        readonly List<string> _filled = new List<string>();
        readonly List<Field> _nothing = new List<Field>();
        readonly List<Field> _shownFields = new List<Field>();
        readonly Func<GridEntity, bool> _fits;
        readonly bool _signalsOnly;
        readonly bool _memberOnly;
        readonly MemberOffers _offers;
        readonly SlotOccupancy _occupancy;
        readonly BlockListing _listing;
        readonly KindFilter _kinds = new KindFilter();

        string _chosen;
        ToolbarSlot _slot;
        string _facet;
        ReachKind _chosenForm;
        string _dropped = "";
        ReachKind _droppedForm;
        Quantifier _quantifier;
        string _whatSearch = "";
        bool _goesOn;
        bool _straight;

        // Whether the column has been shown in the view that lists what the page was opened on, which it is once.
        bool _viewFound;
        string _entered;
        string _shown;
        bool _openedArguments;
        long _anchor;

        // What a pick of blocks has picked by name.
        static readonly IList<string> NoSlots = new string[0];

        // Where the pick names slots of a blueprint: its heading, and the slots listed to be picked by name.
        SlotHeading _slotHeading;
        SlotListing _slotListing;
        bool _retitled;

        PickStrip _strip;
        StageColumn _blocks;
        StageColumn _what;
        StageColumn _does;
        KeyedChildren<long, PickRow> _blockRows;
        KeyedChildren<string, KindRow> _kindRows;
        KeyedChildren<string, Element> _whatRows;
        SlotStrip _slots;
        FacetChoice _facets;
        ListField _reads;
        ICommand _clearKind;
        ICommand _toggleHidden;
        ICommand _organized;
        ICommand _terminal;
        ICommand _finish;

        /// <summary>Asks what to point at and what should happen to it, starting from whatever the answer is now.</summary>
        /// <remarks>
        /// <paramref name="after" /> is the strip of the pick this page goes on in, as its next page, or null for a pick of its
        /// own. <paramref name="admits" /> says which of the blocks able to do what is asked are listed, or null for all of
        /// them. <paramref name="signalsOnly" /> offers only what the construct does to start something, for a signal slot.
        /// <paramref name="forTests" /> offers what an if can test.
        /// <paramref name="facet" /> is the name of the facet the step acts on, where what it does is a member of a type the host
        /// provides.
        /// </remarks>
        public PickPage(SprocketEditor editor, string title, bool forEvents,
            IList<long> targets, string chosen, ReachKind chosenForm, Action<PickAnswer> done,
            bool asksAll = false, Quantifier quantifier = Quantifier.Any, IList<Literal> arguments = null,
            OpeningColumn opens = OpeningColumn.Blocks, OutcomeColumn aim = null, PickStrip after = null, Func<GridEntity, bool> admits = null,
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

            var pressed = forEvents ? SlotOccupancy.Pressing(editor.World, targets, _chosen) : null;
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
            var flipped = !forEvents && !forTests && chosenForm == ReachKind.Action
                ? Vocabulary.Flipped(editor.World, _selection, _chosen)
                : null;
            if (flipped != null)
            {
                _chosen = flipped;
                _arrivedChosen = flipped;
                _chosenForm = ReachKind.Value;
                _arrivedForm = ReachKind.Value;
                _arrivedValues.Clear();
                _arrivedValues.Add(Literal.Of(EntityAction.Toggle));
            }

            _offers = new MemberOffers(editor, forEvents, forTests, signalsOnly);
            _occupancy = new SlotOccupancy(editor);
            _listing = new BlockListing(editor, admits, forEvents, forTests);
        }

        /// <summary>Asks which blocks fill a slot: the blocks alone, only those <paramref name="fits" /> allows, starting from <paramref name="targets" />.</summary>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, Func<GridEntity, bool> fits,
            Action<PickAnswer> done)
            : this(editor, title, false, targets, "", ReachKind.Native, done)
        {
            _fits = fits;
        }

        /// <summary>
        /// Asks which blocks fill a slot of a type the host provides, and which of their facets: the member
        /// <paramref name="member" /> the slot's step does, reached as <paramref name="form" /> says, offered alone, asking
        /// for none of its values, starting from <paramref name="targets" /> and the facet called <paramref name="facet" />,
        /// and only blocks <paramref name="admits" /> allows listed.
        /// </summary>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, string member, ReachKind form, string facet,
            Func<GridEntity, bool> admits, Action<PickAnswer> done)
            : this(editor, title, false, targets, member, form, done, admits: admits, facet: facet)
        {
            _memberOnly = true;
        }

        /// <summary>The slot the pick names where it names one, and otherwise the picker's own name.</summary>
        public override string Title => _slotHeading != null ? _slotHeading.Text : _title;

        /// <summary>
        /// The logic system whose slots the pick names, or null where it names none. For a blueprint the heading shows them: the
        /// slots already made that the picks fill, or the new slot they make, named in place; for any other system, and where
        /// this is null, the heading is the picker's own name. Set before the page is shown.
        /// </summary>
        public AutomationSystem Slotted { get; set; }

        /// <summary>
        /// The signal slot of the trigger being changed, which a pick of what a toolbar sends changes where it matches no signal
        /// slot; empty for a new trigger. Set before the page is shown.
        /// </summary>
        public string Keeps { get; set; } = "";

        /// <summary>Whether the first column lists the slots <see cref="Slotted" /> already has, to be picked by name, rather than blocks. Set before the page is shown.</summary>
        public bool ListsSlots { get; set; }

        /// <summary>The list of slots the pick goes in place of, a step's or a trigger's, or null for a new one. Set before the page is shown.</summary>
        public IList<string> Replacing { get; set; }

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
        /// The step's own values only while its own action is chosen.
        /// </remarks>
        public IList<string> Filled
        {
            get
            {
                _filled.Clear();
                _offering = false;
                if (_arrivedValues.Count > 0 && _chosen == _arrivedChosen && _chosenForm == _arrivedForm)
                {
                    ColorChannels.Shown(_arrivedValues, Wanted(), _filled);
                    return _filled;
                }

                if (_forTests || _forEvents || _selection.Count == 0)
                    return _filled;

                // A value being set opens on what the block holds now, as the terminal's own control does.
                if (_chosenForm == ReachKind.Value)
                {
                    var current = Vocabulary.Current(Editor.World, _selection, _chosen, _facet);
                    if (current.HasValue)
                    {
                        ColorChannels.Shown(new[] { Opaque(current.Value) }, Wanted(), _filled);
                        _offering = true;
                    }
                }
                else
                    ColorChannels.Shown(Opening(Wanted()), Wanted(), _filled);

                return _filled;
            }
        }

        /// <summary>Whether any of <paramref name="fields" /> opens on what the block holds.</summary>
        static bool OpensOnTheBlock(IList<Field> fields)
        {
            foreach (var field in fields)
            {
                if (field.OpensOn != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// What a new step's <paramref name="fields" /> open holding: what the block holds now in the value each opens on, and
        /// nothing in the rest.
        /// </summary>
        List<Literal> Opening(IList<Field> fields)
        {
            var opened = new List<Literal>();
            foreach (var field in fields)
            {
                var current = field.OpensOn == null ? null : Vocabulary.Current(Editor.World, _selection, field.OpensOn, _facet);
                for (var i = 0; i < field.Covers; i++)
                    opened.Add(current.HasValue && field.Covers == 1 ? Opaque(current.Value) : Literal.Of(""));
            }

            return opened;
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
                    if (!BlockListing.Contains(_arrived, id))
                        return true;
                }

                if (_chosen != _arrivedChosen || _chosenForm != _arrivedForm || _quantifier != _arrivedQuantifier
                    || !ToolbarSlot.Same(_slot, _arrivedSlot) || Facet != _arrivedFacet)
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
        /// <remarks>The player's own setting in this world, the same in every picker.</remarks>
        bool ShowsHidden
        {
            get { return Editor.Viewer.ShowsHidden; }
            set
            {
                if (Editor.Viewer.ShowsHidden == value)
                    return;

                Editor.Viewer.ShowsHidden = value;
                if (Editor.Preferred != null)
                    Editor.Preferred();
            }
        }

        bool IsComplete => _selection.Count > 0 && (_fits != null || PicksSignal && (_aim == null || _aim.Chosen)
            || !string.IsNullOrEmpty(_chosen)
            && _offers.ReadinessOf(_chosen, _chosenForm) == Readiness.Ready
            && (_aim == null || _aim.Chosen));

        /// <summary>Whether a signal slot is picked by name, which is all a trigger needs to know of what starts it.</summary>
        bool PicksSignal => _slotListing != null && _slotListing.PicksSignal;

        /// <summary>The type the host provides that a slot picked by name holds, whose members alone are offered; null for any other.</summary>
        HostType SlotType => _slotListing?.Type;

        /// <summary>The slots picked by name, none where blocks are picked.</summary>
        IList<string> PickedSlots => _slotListing != null ? _slotListing.Picked : NoSlots;

        /// <summary>The stages this page asks, which are the last in the pick while it is on top.</summary>
        int Own => (_blocks == null ? 0 : 1) + (_what == null ? 0 : 1) + (_does == null ? 0 : 1);

        /// <summary>The stage whose box takes the keyboard when its page is first shown, or minus one where none does.</summary>
        int Opens => _opens == OpeningColumn.Blocks ? _base : _opens == OpeningColumn.What ? _base + 1 : -1;

        /// <inheritdoc />
        public override bool TakeRetitle()
        {
            var retitled = _retitled;
            _retitled = false;
            return retitled;
        }

        /// <summary>The heading: the slot the pick names, where it names the slots of a blueprint, and otherwise the picker's own name.</summary>
        public override void Heading(List<HeadingPart> parts)
        {
            if (Slotted == null || !Slotted.IsBlueprint)
            {
                base.Heading(parts);
                return;
            }

            if (_slotHeading == null)
                _slotHeading = new SlotHeading(Editor, Slotted, _title, _forEvents, Keeps, Replacing);

            Restate();
            _slotHeading.Head(parts);
        }

        /// <inheritdoc />
        public override void RefreshHeading(List<HeadingPart> parts)
        {
            if (_slotHeading == null)
                base.RefreshHeading(parts);
            else
                _slotHeading.Reword();
        }

        /// <summary>Works out again which slots the picks name; whether the heading changes with it.</summary>
        bool Restate() => _slotHeading.Restate(_selection, _chosen, Facet, _slot ?? ToolbarSlot.Of(_chosen), PickedSlots, ListsSlots);

        /// <summary>Hands <paramref name="answer" /> back, with the name of the slot the heading says the picks make.</summary>
        void Finish(PickAnswer answer)
        {
            answer.SlotName = _slotHeading?.NewName ?? "";
            foreach (var slot in PickedSlots)
                answer.Slots.Add(slot);

            _done(answer);
        }

        /// <summary>Asks before throwing away what was picked here, where there is something to lose and something to do with it.</summary>
        /// <remarks>
        /// Both answers go back; the one Enter takes keeps the work, which finishes the pick as Done does.
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
            if (_forEvents && _chosenForm != ReachKind.Value || !_strip.ShowsArguments || arguments == null || arguments.Form.Count == 0)
            {
                Finish(new PickAnswer(_selection, AnswerId, _quantifier, null, _chosenForm, Facet));
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
            if (_chosenForm == ReachKind.Value && arguments.Count == 1 && arguments[0].Kind == ValueKind.Text
                && arguments[0].Text == EntityAction.Toggle)
            {
                var toggle = Vocabulary.Toggle(Editor.World, _selection, _chosen) ?? _chosen;
                Finish(new PickAnswer(_selection, toggle, _quantifier, new List<Literal>(), ReachKind.Action, Facet));
                return;
            }

            Finish(new PickAnswer(_selection, _chosen, _quantifier, arguments, _chosenForm, Facet));
        }

        /// <summary>Gives the argument of each field for one of a list of names as that name.</summary>
        /// <remarks>The answer comes as a line of text, which carries a chosen word as a bare word, and a bare word reads as text.</remarks>
        void Named(List<Literal> arguments)
        {
            var fields = Fields;
            var at = 0;
            for (var f = 0; f < fields.Count && at < arguments.Count; f++)
            {
                if (fields[f].Entry == FieldInput.Name && arguments[at].Kind == ValueKind.Text)
                    arguments[at] = Literal.Name(arguments[at].Text);

                at += fields[f].Covers;
            }
        }

        /// <inheritdoc />
        public override void Build()
        {
            // A slot of a type the host provides, picked by name, is acted on at the facet it holds.
            if (SlotType != null)
                _facet = _slotListing.Facet;

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

            _strip.Returns = _base > 0 ? Editor.Act(CommandKind.PageBack, PageBack) : null;
            _strip.Onward = _aim != null && _aim.Outcome == Outcome.One ? Editor.Act(CommandKind.PageOn, Primary) : null;

            if (_finish == null)
                _finish = Editor.Act(CommandKind.Primary, Primary);

            if (_blocks == null)
                MakeBlocks();

            FillBlocks();



            // The second stage exists only once there is something for it to be about, and never where only blocks are asked.
            var wanted = _selection.Count > 0 && _fits == null && !PicksSignal;
            if (wanted && _what == null)
            {
                // The stages stand in order, so the column of what the trigger does is taken off and made again after this one.
                if (_does != null)
                {
                    _strip.RemoveLast();
                    _does = null;
                }

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

            // What the trigger does, beside what happens, once what happens is chosen or a signal slot is picked.
            var does = _aim != null && (PicksSignal || _what != null && !string.IsNullOrEmpty(_chosen));
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

            if (_slotHeading != null && Restate())
                _retitled = true;

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
        }

        /// <summary>Goes straight on to the page after this one as soon as this is built, for a part pressed that stands for something on that page.</summary>
        /// <remarks>Built first all the same, because the page after carries on from the stages this one asks.</remarks>
        public void GoOn()
        {
            _goesOn = true;
        }

        /// <summary>
        /// The choice between this and a selector of the logic system's states, offered in the heading of the blocks column,
        /// or null where the pick is about blocks alone. Set before the page is shown.
        /// </summary>
        public SourceSwitch Source { get; set; }

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
            _blocks.Title = Source != null ? Editor.Texts.PickSource : ListsSlots ? Editor.Texts.PickSlots : Editor.Texts.PickBlocks;
            if (Source != null)
                _blocks.Heads(Source.Box(ListsSlots ? PickSource.Slots : PickSource.Blocks));

            _blocks.EmptyText = ListsSlots
                ? Editor.Texts.PickNoSlots
                : _fits != null
                ? Editor.Texts.BlueprintNoneFits
                : _forTests ? Editor.Texts.PickNoBlocksChecked
                : _forEvents ? Editor.Texts.PickNoBlocksHappen : Editor.Texts.PickNoBlocksDone;

            // For the whole life of the column, so the line does not come and go as the list of kinds is narrowed.
            _blocks.HasKinds = !ListsSlots && _kinds.Present.Count > 0;
            _blocks.Narrowed += OnBlockSearch;
            _blocks.KindsNarrowed += NarrowKinds;

            if (ListsSlots)
                _slotListing = new SlotListing(Editor, Slotted, _forEvents, _selection, MoveOn);
            else
                _blockRows = new KeyedChildren<long, PickRow>(_blocks.Rows, BlockRow);

            _kindRows = new KeyedChildren<string, KindRow>(_blocks.Kinds, KindRow);
            _blocks.KindsOpened += delegate
            {
                if (Editor.Input != null)
                    Editor.Input.Focus(_blocks.KindNarrow);
            };
            _clearKind = Editor.Act(CommandKind.AnyKind, () => SetKind(""));
            _toggleHidden = Editor.Act(CommandKind.HiddenBlocks, delegate
            {
                ShowsHidden = !ShowsHidden;
                Editor.Invalidate();
            });
            _blocks.ShowHidden = ListsSlots ? null : _toggleHidden;

            _strip.Add(_blocks);
        }

        void MakeWhat()
        {
            // Two views of what is done to a block, checked of one or happens to one: what Sprocket vouches for, and the rest.
            // The angle marks point the way each link goes; the words alone are translated.
            var views = new[] { $"‹ {Editor.Texts.PickToOrganized}", $"{Editor.Texts.PickToTerminal} ›" };

            _what = new StageColumn(_strip.Width, true, views, Editor.Texts);
            _what.Title = _forTests ? Editor.Texts.PickWhatToCheck : _forEvents ? Editor.Texts.PickWhatHappens : Editor.Texts.PickWhatToDo;
            _what.EmptyText = _forTests
                ? Editor.Texts.PickNothingChecked
                : _forEvents ? Editor.Texts.PickNothingHappens : Editor.Texts.PickNothingDone;
            _what.Narrowed += OnWhatSearch;
            _whatRows = new KeyedChildren<string, Element>(_what.Rows, WhatRow);

            _organized = Editor.Act(CommandKind.ShowOrganized, () => Show(false));
            _terminal = Editor.Act(CommandKind.ShowTerminal, () => Show(true));

            _strip.Add(_what);
        }

        PickRow BlockRow(long id)
        {
            var row = new PickRow();
            row.Picked += (held, presses) => Picked(id, held, presses);
            row.Ticks = Editor.Act(CommandKind.PickEntity, () => Pick(id, HeldModifier.Also));
            return row;
        }

        KindRow KindRow(string kind)
        {
            var family = KindFilter.FamilyOf(kind);

            return new KindRow
            {
                Command = Editor.Act(kind.Length == 0 ? CommandKind.AnyKind : CommandKind.OneKind, delegate
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
            row.Picked += delegate(HeldModifier held, int presses) { Chosen(key, presses); };
            return row;
        }

        /// <summary>Takes a block the way the press said, and moves on where a plain press was a second in quick succession.</summary>
        void Picked(long id, HeldModifier held, int presses)
        {
            var pick = Editor.Act(CommandKind.PickEntity, () => Pick(id, held));
            if (pick == null)
                return;

            pick.Execute();
            if (presses > 1 && held == HeldModifier.Alone)
                MoveOn();
        }

        /// <summary>Chooses what the row under <paramref name="key" /> offers, and moves on where it was pressed twice in quick succession.</summary>
        void Chosen(string key, int presses)
        {
            int at;
            if (!_whatAt.TryGetValue(key, out at) || at < 0)
                return;

            var id = _offers.Ids[at];
            var form = _offers.Forms.Count > at ? _offers.Forms[at] : ReachKind.Native;
            var choose = Editor.Act(CommandKind.PickChoice, () => Choose(id, form));
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
            if (_forEvents && _chosenForm == ReachKind.Value)
                return ComparisonQuestion.Fields(Valued(Vocabulary.ActionsOf(Editor.World, _selection), _chosen), Vocabulary.EventsOf(Editor.World, _selection));

            if (!_forTests)
                return Editor.Fields(_selection, _chosen, _chosenForm);

            string value, state;
            return ComparisonQuestion.Split(_chosen, out value, out state)
                ? ComparisonQuestion.StateFields()
                : ComparisonQuestion.Fields(Valued(Vocabulary.ActionsOf(Editor.World, _selection), _chosen), Vocabulary.EventsOf(Editor.World, _selection));
        }

        /// <summary>
        /// Says what the chosen thing takes, and what it opens holding, where either has changed; and puts the values beside
        /// the stages once something that can be finished is chosen.
        /// </summary>
        void FillGiven()
        {
            // A moment takes nothing; a number watched asks how it compares, and with what, over what the trigger does.
            if (_forEvents && _chosenForm != ReachKind.Value || _memberOnly)
            {
                _strip.ShowsArguments = false;
                return;
            }

            _strip.ArgumentsIn = _forEvents ? _does ?? _what : null;

            var shown = IsComplete ? $"{_chosenForm} {_chosen}" : "";
            _strip.ShowsArguments = _what != null && shown.Length != 0;

            // The fields are compared too, since they change with the blocks: a hinge picked beside a rotor turns without a
            // direction.
            var fields = Wanted();

            // Values that open on what the block holds are each facet's own, read off the one chosen.
            if (shown.Length != 0 && _facet.Length != 0 && OpensOnTheBlock(fields))
                shown = $"{shown} {_facet}";
            if (_shown == shown && Same(_shownFields, fields))
                return;

            _shown = shown;
            _shownFields.Clear();
            _shownFields.AddRange(fields);

            // Read once, where a color is asked for: these are the colors this player paints with, only ever read.
            var colors = new List<Hsv>();
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Edits != FieldEditor.Colour)
                    continue;

                Player.Colours(colors);
                break;
            }

            var made = _strip.Arguments == null;
            var arguments = _strip.Restate(shown, Chosen(), fields, Filled, new ColorOffer(colors, Editor.Viewer, Editor.Preferred));
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
            if (_opens == OpeningColumn.Arguments && !_openedArguments && arguments.Form.Count > 0)
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
                if (action.Id == id && (action.Sets || action.OwnValue != null))
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
            if (ListsSlots)
            {
                _slotListing.Show(_blocks, _screen.Search);
                FillOptions();
                _blocks.ShowEmpty();
                return;
            }

            var texts = Editor.Texts;
            _blocks.Subtitle = _selection.Count == 0
                ? texts.PickNonePicked
                : _selection.Count == 1
                    ? texts.PickOnePicked
                    : Template.Fill(texts.PickSomePicked, "count", _selection.Count.ToString(CultureInfo.InvariantCulture));

            // Offered whatever the length of the list.
            _blocks.NarrowText = _screen.Search;

            // Off the whole catalogue, so narrowing never takes the picture column away and shifts every row sideways.
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
                row.Secondary = _listing.Ahead.TryGetValue(entity.Id, out note) ? note : Kind(entity);
                row.Icon = entity.Kind.Icon;
                row.HasPicture = icons;
                row.Many = true;
                row.IsChosen = BlockListing.Contains(_selection, entity.Id);
                row.Show();
            }

            _blocks.ShowEmpty();
        }

        /// <summary>The kind of <paramref name="entity" />, and the grid it is on where that is not the programmable block's own.</summary>
        string Kind(GridEntity entity)
        {
            var home = Editor.World.Grid;
            var grid = entity.Block.CubeGrid;
            if (home == null || grid == null || grid == home)
                return entity.Kind.DisplayName;

            return Template.Fill(Template.Fill(Editor.Texts.PickOnGrid, "kind", entity.Kind.DisplayName), "grid", grid.CustomName);
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
        void NarrowKinds(string said)
        {
            said = said ?? "";
            if (said == _kinds.Search)
                return;

            var narrow = Editor.Act(CommandKind.NarrowKinds, delegate
            {
                _kinds.Search = said;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>
        /// Puts the choice of how several blocks are read under them, on a test and on a trigger watching for a state.
        /// </summary>
        void FillOptions()
        {
            var asks = _asksAll && (_forTests || _forEvents && !_signalsOnly && (_chosen.Length == 0 || Watched() || _chosenForm == ReachKind.Value || Totals));
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
                    var reads = Editor.Act(CommandKind.ReadBlocks, () => Quantify(Read(_reads.Text)));
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
            FindView();

            var texts = Editor.Texts;
            var note = "";
            if (_forTests || _forEvents)
            {
                note = Editor.ShowsTerminal ? texts.PickTerminalNote : "";
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

            // A choice the blocks can no longer honor is dropped, so Done never shows over a sentence that could not run, and
            // kept aside until something else is chosen: blocks that honor it again bring it back with its values.
            if (_chosen.Length > 0 && _offers.At(_chosen, _chosenForm) < 0)
            {
                _dropped = _chosen;
                _droppedForm = _chosenForm;
                _chosen = "";
                _chosenForm = ReachKind.Native;
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
                // A slot of a type the host provides offers that type's members alone, on the facet the slot holds.
                if (SlotType != null && _offers.Types[i] != SlotType)
                    continue;

                // What is chosen stays listed whatever the search, as a chosen block does, so a search never hides the
                // answer this picker opened with.
                if (EntityQuery.MatchesText(_offers.Names[i], _whatSearch) || _offers.Ids[i] == _chosen)
                    _matched.Add(i);
            }

            // A heading before the first of each group that survived the search. An event list has no groups.
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
                    : $"{(_offers.Forms.Count > at ? _offers.Forms[at] : ReachKind.Native)} {_offers.Ids[at]}";

                _whatKeys.Add(key);
                _whatAt[key] = at;
            }

            _whatShown.Clear();
            _whatShown.AddRange(_whatKeys);

            // Which of the blocks' screens, or of whatever else of a type the host provides, its members act on is chosen
            // first: a drop box heads them where the blocks share more than one, and each stands in under it.
            var first = _forEvents ? -1 : FirstTyped();
            var type = first < 0 ? null : _offers.Types[_rows[first]];
            var shared = type == null || SlotType != null ? null : Facets.Shared(Editor.World, _selection, type);
            if (shared != null)
                Keep(shared);

            var facets = shared != null && shared.Count > 1;
            if (facets)
                _whatShown.Insert(first, FacetsKey);

            // The slots of a toolbar a person presses open under its line once it is chosen.
            var pressed = SlotOccupancy.Pressing(Editor.World, _selection, _chosen);
            var under = pressed == null ? -1 : _whatShown.IndexOf($"{ReachKind.Native} {pressed.Id}");
            if (under >= 0)
                _whatShown.Insert(under + 1, SlotsKey);

            _whatRows.Show(_whatShown);
            if (under >= 0)
                ShowSlots(pressed);
            else
                _slots = null;

            if (facets)
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
                row.Indented = facets && at >= 0 && _offers.Types[at] == type;

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
                var form = _offers.Forms.Count > at ? _offers.Forms[at] : ReachKind.Native;

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

        /// <summary>
        /// Shows, the first time the column is filled, the view that lists what the page was opened on, so whatever made a
        /// choice edits it; the view showing stays where it lists it, or where neither does.
        /// </summary>
        void FindView()
        {
            if (_viewFound)
                return;

            _viewFound = true;
            if (_chosen.Length == 0 || _offers.At(_chosen, _chosenForm) >= 0)
                return;

            Editor.ShowsTerminal = !Editor.ShowsTerminal;
            _offers.Fill(_selection, Totals, _memberOnly ? _arrivedChosen : null);
            if (_offers.At(_chosen, _chosenForm) >= 0)
                return;

            Editor.ShowsTerminal = !Editor.ShowsTerminal;
            _offers.Fill(_selection, Totals, _memberOnly ? _arrivedChosen : null);
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
        void Pick(long entityId, HeldModifier how)
        {
            if (how == HeldModifier.Through && _anchor != 0L)
            {
                Span(_anchor, entityId);
                Editor.Invalidate();
                return;
            }

            _anchor = entityId;

            if (how == HeldModifier.Also)
                Toggle(entityId);
            else
                Only(entityId);

            Editor.Invalidate();
        }

        void Toggle(long entityId)
        {
            if (BlockListing.Contains(_selection, entityId))
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

        // Keys among the rows that no row's own key can be.
        const string SlotsKey = "slots";

        const string FacetsKey = "facets";

        /// <summary>What is chosen as the program names it: the one slot, where a toolbar a person presses is chosen.</summary>
        string AnswerId => _slot == null ? _chosen : _slot.EventId;

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

                    Chosen($"{ReachKind.Native} {pressed.Id}", 1);
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

            var pick = Editor.Act(CommandKind.PickChoice, delegate
            {
                _slot = new ToolbarSlot(whole.Toolbar, page, slot);
                Editor.Invalidate();
            });
            if (pick != null)
                pick.Execute();
        }

        /// <summary>Where the first member of a type the host provides stands among the rows, or -1 where none is listed.</summary>
        int FirstTyped()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                var at = _rows[i];
                if (at >= 0 && _offers.Types[at] != null)
                    return i;
            }

            return -1;
        }

        /// <summary>The facet the chosen member acts on, or empty where it is no member of a type the host provides.</summary>
        string Facet => Vocabulary.TypeOf(Editor.World, _selection, _chosen) == null ? "" : _facet;

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
            var pick = Editor.Act(CommandKind.PickChoice, delegate
            {
                _facet = name;
                Editor.Invalidate();
            });
            if (pick != null)
                pick.Execute();
        }

        void Choose(string id, ReachKind form)
        {
            var pressed = form == ReachKind.Native ? SlotOccupancy.Pressing(Editor.World, _selection, id) : null;
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
                && (ComparisonQuestion.Split(_chosen, out value, out state) || !MemberOffers.Number(Valued(Vocabulary.ActionsOf(Editor.World, _selection), _chosen))))
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

            var narrow = Editor.Act(CommandKind.NarrowEntities, delegate
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

            var narrow = Editor.Act(CommandKind.NarrowChoices, delegate
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
