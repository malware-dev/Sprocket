using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// Picks blocks and then what happens to them, with both stages on screen at once.
    /// </summary>
    /// <remarks>
    /// This replaces a pair of screens shown one after the other, which hid the first
    /// answer the moment it asked the second. Side by side, the whole path reads at a
    /// glance and changing an earlier answer is a glance to the left.
    ///
    /// Choices apply as they are made - the program is live, so there is nothing to
    /// commit. "Done" exists only to say the user has finished, because going back has
    /// to keep meaning "return to where I came from".
    /// </remarks>
    sealed class PickPage : EditorPage
    {
        readonly string _title;
        readonly List<long> _selection = new List<long>();
        readonly bool _forEvents;
        readonly bool _asksAll;
        readonly Action<PickAnswer> _done;

        string _chosen;

        Form _chosenForm;
        bool _all;

        readonly List<GridEntity> _candidates = new List<GridEntity>();
        readonly List<GridEntity> _filtered = new List<GridEntity>();

        readonly Dictionary<long, int> _slots = new Dictionary<long, int>();

        static readonly NameOrder ByName = new NameOrder();

        /// <remarks>
        /// A comparer object rather than a <c>Comparison</c> delegate, which Space Engineers does not
        /// whitelist.
        /// </remarks>
        sealed class NameOrder : IComparer<GridEntity>
        {
            public int Compare(GridEntity a, GridEntity b)
            {
                // Neither can be null: the only lists sorted with this are the world's own entities,
                // and nothing ever puts a null in one. The interface allows it; this codebase does not.
                // ReSharper disable PossibleNullReferenceException
                var byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.Id.CompareTo(b.Id);
                // ReSharper restore PossibleNullReferenceException
            }
        }
        readonly List<EntityKind> _kinds = new List<EntityKind>();

        readonly Filter _filter;

        string _blockSearch;
        string _kind;

        string _kindSearch;

        readonly List<EntityKind> _shownKinds = new List<EntityKind>();
        string _whatSearch;
        readonly List<string> _names = new List<string>();
        readonly List<string> _ids = new List<string>();
        readonly List<int> _matched = new List<int>();

        PickComponent _pick;
        PickStage _blocks;
        PickStage _what;

        /// <summary>
        /// Asks what to point at and what should happen to it, starting from whatever the
        /// answer is now.
        /// </summary>
        /// <remarks>
        /// The page keeps its own answer and hands it back when somebody says they are done. Ticking
        /// entries straight into the step would make a tentative tap an edit to a sequence that might
        /// be running - the system acting on a decision nobody had finished making - and would leave
        /// back with nothing to back out of.
        /// <para>
        /// Live editing is still the rule for the program as a whole: there is no save button anywhere
        /// and there should not be. It just cannot mean that every tap in the middle of choosing is
        /// committed.
        /// </para>
        /// </remarks>
        public PickPage(SprocketEditor editor, string title, bool forEvents,
            IList<long> targets, string chosen, Form chosenForm, Action<PickAnswer> done,
            bool asksAll = false, bool all = false, IList<Literal> values = null,
            Opening opens = Opening.Blocks, Aim aim = null, PickComponent after = null)
            : base(editor)
        {
            _opens = opens;
            _aim = aim;
            if (aim != null)
                aim.Onward = Primary;

            // Going on in the pick that came before, as its next page.
            _pick = after;
            _base = after == null ? 0 : after.Stages.Count;

            if (values != null)
            {
                for (var i = 0; i < values.Count; i++)
                    _arrivedValues.Add(values[i]);
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

            // Opened the way the last one of its sort was left. What is on this construct is not known
            // yet, so a kind it does not have is dropped later, where it is - see FillFilters.
            _filter = editor.Habits.For(forEvents);
            _kind = _filter.Kind;
            _blockSearch = _filter.Blocks;
            _kindSearch = _filter.Kinds;
            _whatSearch = _filter.Choices;

            for (var i = 0; i < targets.Count; i++)
            {
                _selection.Add(targets[i]);
                _arrived.Add(targets[i]);
            }
        }

        readonly List<long> _arrived = new List<long>();

        string _said;

        readonly List<long> _saidBlocks = new List<long>();

        string _saidChosen = "";
        int _saidOffered;
        bool _saidAll;

        bool _retitled;

        /// <summary>The title as the answer stands, or the picker's own name where nothing is chosen.</summary>
        public override string Title => _said ?? _title;

        /// <inheritdoc />
        public override bool TakeRetitle()
        {
            Reword();

            var retitled = _retitled;
            _retitled = false;
            return retitled;
        }

        /// <inheritdoc />
        /// <remarks>Nothing where what the trigger does goes on to a page of its own.</remarks>
        public override string PrimaryText => IsComplete && (_aim == null || _aim.Finishes) ? "Done" : "";

        /// <summary>
        /// What the chosen action will not do anything without, asked here rather than on a screen
        /// of its own.
        /// </summary>
        /// <remarks>
        /// Nearly nothing wants values, so this is empty nearly always. When it is not, the count is
        /// the whole of what can honestly be asked: the game declares how many values an action
        /// takes and nothing about what any of them is for.
        /// </remarks>
        public override string PrimaryQuestion
        {
            get
            {
                List<Field> fields = Fields();
                return fields.Count > 0 ? SequenceText.Wants(fields) : "";
            }
        }

        public override IList<Field> PrimaryFields => Fields();

        /// <summary>
        /// What the values question opens holding, where this opened on something that has values.
        /// </summary>
        /// <remarks>
        /// A picker opened to change which blocks a step acts on already has that step's values, and
        /// opening the question empty would make somebody type them again to keep them. Worked out
        /// the way a step's own chip works them out, so the two say the same thing about one value.
        /// </remarks>
        public override IList<string> PrimaryFilled
        {
            get
            {
                // Only for what the step did. Another action's values are not these, whatever their
                // count happens to be.
                _filled.Clear();
                if (_arrivedValues.Count > 0 && _chosen == _arrivedChosen && _chosenForm == _arrivedForm)
                    Channels.Shown(_arrivedValues, Fields(), _filled);

                return _filled;
            }
        }

        readonly List<Literal> _arrivedValues = new List<Literal>();

        readonly Opening _opens;

        bool _goesOn;

        bool _straight;

        readonly Aim _aim;

        readonly int _base;

        PickStage _does;

        readonly string _arrivedChosen;

        readonly Form _arrivedForm;

        readonly bool _arrivedAll;

        string _entered;

        readonly List<string> _filled = new List<string>();

        /// <summary>
        /// Asks before throwing away what was picked here.
        /// </summary>
        /// <remarks>
        /// Back means back, and both answers go back - the question is only about what survives it.
        /// So the one Enter takes is the one that keeps the work, because a key pressed without
        /// reading must not be the one that loses it.
        /// <para>
        /// Only where there is something to lose and something to do with it. A picker opened and
        /// read asks nothing, and nor does one still waiting to be told what happens - a picker
        /// that cannot be finished has no answer that keeps anything, and leaves without a word.
        /// <para>
        /// Keeping is finishing. Where finishing needs values the values are asked for, exactly as
        /// they are when the button is pressed, because the two are the same act arrived at from
        /// two directions.
        /// </para>
        /// </para>
        /// <para>
        /// The same question whichever way somebody arrived, so there is no rule to remember about
        /// how they got here.
        /// </para>
        /// </remarks>
        public override bool Leaving()
        {
            // Back retraces the way here: through the pick where it was paged through, and out of it
            // where it was opened straight on this page.
            if (_base > 0)
                return _straight ? LeavingStraight() : true;

            var after = _aim == null ? null : _aim.Following;
            if (!IsComplete || !Moved && (after == null || !after.Moved))
                return true;

            Editor.Ask("Keep your changes?");

            Editor.AnswerPrimary("Keep them", true);
            Editor.Answer("Discard them", delegate { Editor.Pop(); });

            return false;
        }

        /// <summary>
        /// Whether anything here differs from what was already chosen when it opened: the blocks, what
        /// is done to them, whether any or all of them, or the values it is done with.
        /// </summary>
        internal bool Moved
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

        /// <summary>
        /// Whether the values hold anything other than what the step was given.
        /// </summary>
        /// <remarks>
        /// By value rather than by spelling: a field shows a real number with a place after the point,
        /// so a step given 5 opens showing 5.0, which is the same value and no change.
        /// </remarks>
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

        static bool Same(Literal a, Literal b)
        {
            var numbers = (a.Kind == ValueKind.Integer || a.Kind == ValueKind.Real)
                && (b.Kind == ValueKind.Integer || b.Kind == ValueKind.Real);

            return numbers ? a.Number == b.Number : a.Kind == b.Kind && a.Said == b.Said;
        }

        void OnEntered(string said)
        {
            _entered = said ?? "";
        }

        public override void Primary()
        {
            _done(new PickAnswer(_selection, _chosen, _all, null, ChosenForm));
        }

        public override void Primary(string said)
        {
            var values = new List<Literal>();
            ValueLine.Read(said, values);
            _done(new PickAnswer(_selection, _chosen, _all, values, ChosenForm));
        }

        /// <summary>Which of the three ways the thing chosen reaches the blocks.</summary>
        Form ChosenForm => _chosenForm;

        List<Field> Fields()
        {
            return IsComplete ? Editor.Fields(_selection, _chosen, _chosenForm) : _nothing;
        }

        readonly List<Field> _nothing = new List<Field>();

        bool IsComplete => _selection.Count > 0 && !string.IsNullOrEmpty(_chosen)
            && ReadinessOf(_chosen, _chosenForm) == Readiness.Ready
            && (_aim == null || _aim.Chosen);

        /// <inheritdoc />
        public override void Build(List<EditorComponent> components)
        {
            Collect();

            if (_pick == null)
                _pick = new PickComponent();

            if (_aim != null)
                _aim.Pick = _pick;

            // Back from a page after this one, whatever that page asked goes with it; and back here from
            // the page before, what this one asked is put back.
            var changed = _pick.Return(_base, _base + Own);
            if (_pick.Stages.Count == _base && _blocks != null)
            {
                _pick.Add(_blocks);
                if (_what != null)
                    _pick.Add(_what);

                if (_does != null)
                    _pick.Add(_does);
            }

            _pick.Fills = true;
            _pick.Returns = _base > 0 ? Editor.Register(Cmd.PageBack, PageBack) : 0;
            _pick.Onward = _aim != null && _aim.Outcome == Outcome.One ? Editor.Register(Cmd.PageOn, Primary) : 0;

            // A step is finished by giving what it does its values. What happens to a block takes
            // none, so a trigger is finished the way it always was.
            _pick.Finishes = !_forEvents;
            _pick.OpensOnValues = _opens == Opening.Values;
            _pick.Opens = _opens == Opening.Blocks ? _base : _opens == Opening.What ? _base + 1 : -1;
            _pick.EnteredCommand = _forEvents ? 0 : Editor.RegisterText(Cmd.EnterValues, OnEntered);

            if (_blocks == null)
            {
                _blocks = new PickStage("Blocks");
                _blocks.AllowsMany = true;
                _blocks.EmptyText = _forEvents
                    ? "Nothing here has anything that can happen to it."
                    : "Nothing here has anything that can be done to it.";
                _pick.Add(_blocks);
            }

            changed |= FillBlocks();

            // The second stage exists only once there is something for it to be about.
            var wanted = _selection.Count > 0;
            if (wanted && _what == null)
            {
                _what = new PickStage(_forEvents ? "What happens" : "What to do");

                // Only over what somebody does to a block. What happens to one is Sprocket's own
                // wording throughout, so there is no second way to look at it.
                if (!_forEvents)
                {
                    _what.Views.Add(new ChoiceEntry(OrganizedSaid, true, 0));
                    _what.Views.Add(new ChoiceEntry(TerminalSaid, false, 0));
                }
                _what.EmptyText = _forEvents
                    ? "Those blocks have nothing in common that can happen to them."
                    : "Those blocks have nothing in common that can be done to them.";
                _pick.Add(_what);
            }
            else if (!wanted && _what != null)
            {
                if (_does != null)
                {
                    _pick.RemoveLast();
                    _does = null;
                }

                _pick.RemoveLast();
                _what = null;
            }

            if (_what != null)
                changed |= FillWhat();

            // What the trigger does, beside what happens, once what happens is chosen.
            var does = _what != null && _aim != null && !string.IsNullOrEmpty(_chosen);
            if (does && _does == null)
            {
                _does = new PickStage("What it does");
                _does.Finishes = true;
                _pick.Add(_does);
            }
            else if (!does && _does != null)
            {
                _pick.RemoveLast();
                _does = null;
            }

            changed |= FillGiven();

            if (_does != null && _aim != null)
                changed |= _aim.Fill(_does);

            if (changed)
                _pick.Changed();

            components.Add(_pick);

            if (!_goesOn)
                return;

            _goesOn = false;
            Primary();
        }

        /// <summary>
        /// Goes straight on to the page after this one as soon as this is built, for a part pressed
        /// that stands for something on that page.
        /// </summary>
        /// <remarks>
        /// Built first all the same, because the page after carries on from the stages this one asks.
        /// The editor never shows a page that went on while it was being built.
        /// </remarks>
        internal void GoOn()
        {
            _goesOn = true;
        }

        /// <summary>Says whether this page is being opened straight onto, rather than paged to.</summary>
        internal void Arrived(bool straight)
        {
            _straight = straight;
        }

        /// <summary>
        /// Pages back to the page before, which is what the way back beside the columns does however
        /// this page was come to - and from then on this page was paged to.
        /// </summary>
        void PageBack()
        {
            _straight = false;
            Editor.Pop();
        }

        /// <summary>
        /// Leaves the pick from a page opened straight onto, past the page before that was never
        /// looked at, asking first where anything here has changed.
        /// </summary>
        bool LeavingStraight()
        {
            if (!IsComplete || !Moved)
            {
                Editor.Pop();
                return true;
            }

            Editor.Ask("Keep your changes?");

            Editor.AnswerPrimary("Keep them", true);
            Editor.Answer("Discard them", delegate
            {
                Editor.Pop();
                Editor.Pop();
            });

            return false;
        }

        /// <summary>The stages this page asks, which are the last in the pick while it is on top.</summary>
        int Own => (_blocks == null ? 0 : 1) + (_what == null ? 0 : 1) + (_does == null ? 0 : 1);

        const int Looked = 15;

        Interval _looked = new Interval(Looked);

        internal override void Resay()
        {
            base.Resay();

            // This page's own clock, which the base knows nothing about. A picker arriving on screen
            // reads what the blocks are doing now rather than carrying over what they were doing when
            // it was last looked at.
            _looked.Restart();
        }

        /// <summary>
        /// Brings live state up to date, and says nothing when there is nothing to say.
        /// </summary>
        /// <remarks>
        /// On a cadence, because nothing in the game announces a door opening or a vent filling - this
        /// is the one thing here that has to be asked for. It compares before it writes, so a tick
        /// where nothing moved says nothing and the screen does no work at all.
        /// </remarks>
        public override void Refresh()
        {
            if (_blocks == null)
                return;

            if (!_looked.Due(Editor.CurrentTick))
                return;

            var changed = false;
            for (var i = 0; i < _blocks.Entries.Count && i < _filtered.Count; i++)
            {
                var entity = Editor.World.FindEntity(_filtered[i].Id);
                if (entity == null)
                    continue;

                var entry = _blocks.Entries[i];
                if (entry.Status != entity.Status)
                {
                    entry.Status = entity.Status;
                    changed = true;
                }

                if (entry.IsActive != entity.IsActive)
                {
                    entry.IsActive = entity.IsActive;
                    changed = true;
                }
            }

            if (changed)
                _pick.Changed();
        }

        /// <summary>
        /// Says what the chosen thing takes, and what it opens holding, where either has changed.
        /// </summary>
        /// <remarks>
        /// The same thing can be asked for differently as the blocks change - a hinge picked beside a
        /// rotor turns without a direction - so the fields are compared as well as what they are for.
        /// </remarks>
        bool FillGiven()
        {
            if (!_pick.Finishes)
                return false;

            var given = IsComplete ? _chosenForm + " " + _chosen : "";
            var fields = Fields();
            if (_pick.Given == given && Same(_pick.Fields, fields))
                return false;

            _pick.Given = given;
            _pick.Fields.Clear();
            _pick.Fields.AddRange(fields);
            _pick.Filled.Clear();
            _pick.Filled.AddRange(PrimaryFilled);
            _pick.Restate();
            return true;
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

        bool FillBlocks()
        {
            var changed = false;

            var subtitle = _selection.Count == 0
                ? "none picked"
                : _selection.Count == 1 ? "1 picked" : _selection.Count + " picked";

            if (_blocks.Subtitle != subtitle)
            {
                _blocks.Subtitle = subtitle;
                changed = true;
            }

            // Always offered, never conditioned on how long the list is. Whether a way to
            // narrow is worth showing - and at what point a list is long enough to need one -
            // is the target looking at its own screen, not core counting rows.
            if (_blocks.NarrowText != _blockSearch)
            {
                _blocks.NarrowText = _blockSearch;
                changed = true;
            }

            var narrow = Editor.RegisterText(Cmd.NarrowEntities, OnBlockSearch);
            if (_blocks.NarrowCommand != narrow)
            {
                _blocks.NarrowCommand = narrow;
                changed = true;
            }

            // Off the whole catalogue, not the filtered rows: narrowing must not take the
            // picture column away and shuffle every row sideways.
            var icons = false;
            for (var i = 0; i < _candidates.Count && !icons; i++)
                icons = !string.IsNullOrEmpty(_candidates[i].Kind.Icon);

            if (_blocks.HasIcons != icons)
            {
                _blocks.HasIcons = icons;
                changed = true;
            }

            changed |= FillFilters();
            changed |= FillOptions();

            changed |= Resize(_blocks.Entries, _filtered.Count);
            for (var i = 0; i < _filtered.Count; i++)
            {
                var entity = _filtered[i];
                var id = entity.Id;

                changed |= Apply(_blocks.Entries[i], entity.Name, entity.Kind.DisplayName,
                    entity.Status, entity.IsActive, Contains(_selection, id),
                    Editor.RegisterNumber(Cmd.PickEntity, Slot(id),
                        delegate(int how) { Pick(id, (Held)how); }),
                    entity.Kind.Icon);
            }

            return changed;
        }

        /// <summary>
        /// The number this entity is known by on this page, assigned the first time it is seen.
        /// </summary>
        /// <remarks>
        /// Never reused and never reassigned, so an id a target is holding keeps meaning the same block for
        /// as long as the page is open. Leaving and returning builds a fresh page and a fresh map, which is
        /// fine: nothing holds an id across screens.
        /// </remarks>
        int Slot(long id)
        {
            int slot;
            if (_slots.TryGetValue(id, out slot))
                return slot;

            slot = _slots.Count;
            _slots.Add(id, slot);
            return slot;
        }

        bool FillFilters()
        {
            // A kind remembered from an earlier picker may not be on this construct at all, and one
            // that is not would hide every block with nothing on screen to say why. It is dropped
            // rather than kept, here, where what this construct actually has is known.
            if (_kind.Length > 0 && !HasKind(_kind))
                SetKind("");

            _shownKinds.Clear();
            for (var i = 0; i < _kinds.Count; i++)
            {
                // The one in force is always offered, however the box is narrowed, so that what the
                // line says is showing is always something the list can be seen to say too.
                if (_kinds[i].Id == _kind || EntityQuery.MatchesText(_kinds[i].DisplayName, _kindSearch))
                    _shownKinds.Add(_kinds[i]);
            }

            var changed = Resize(_blocks.Filters, _shownKinds.Count + 1);

            // One registration, used twice: the row that says anything and the way out beside the
            // line are the same act, and a command's identity has to be unique on its screen.
            int anything = Editor.Register(Cmd.AnyKind, ClearKind);

            changed |= Apply(_blocks.Filters[0], "Anything", _candidates.Count, _kind.Length == 0,
                anything);

            for (var i = 0; i < _shownKinds.Count; i++)
            {
                var id = _shownKinds[i].Id;
                changed |= Apply(_blocks.Filters[i + 1], _shownKinds[i].DisplayName, CountOfKind(id),
                    _kind == id, Editor.Register(Cmd.OneKind, i, delegate { SetKind(id); }));
            }

            _blocks.KindText = _kindSearch;
            _blocks.KindCommand = Editor.RegisterText(Cmd.NarrowKinds, NarrowKinds);
            _blocks.IsNarrowed = _kind.Length > 0;
            _blocks.ClearCommand = anything;

            return changed;
        }

        bool HasKind(string kindId)
        {
            for (var i = 0; i < _kinds.Count; i++)
            {
                if (_kinds[i].Id == kindId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Narrows the kinds on offer, and nothing else.
        /// </summary>
        /// <remarks>
        /// The box over the kinds and the box over the blocks are two questions: one asks which
        /// sort, the other asks which one. Typing into either while the other holds something is an
        /// ordinary thing to do, so neither clears the other.
        /// </remarks>
        void NarrowKinds(string said)
        {
            _kindSearch = said ?? "";
            _filter.Kinds = _kindSearch;
            Editor.Invalidate();
        }

        const bool AsksAll = false;

        /// <summary>
        /// Offered wherever the distinction means anything - a step always acts on all its blocks, so
        /// only some stages ask at all.
        /// </summary>
        /// <remarks>
        /// Stated for the whole life of such a stage, and worded the same throughout. Waiting until
        /// there was more than one thing picked was worse in two ways at once: the choice arrived
        /// under the pointer at the moment somebody was reaching for a third block, and holding the
        /// room for it instead only traded that for a hole sitting open the rest of the time. Short
        /// enough to be one line, and a setting somebody can see is one they can change before it
        /// matters rather than after.
        /// </remarks>
        bool FillOptions()
        {
            // The choice between any one of the blocks and all of them stays off the screen. A trigger that waits
            // for all of its blocks is built and tested, but the choice would be offered on triggers it means
            // nothing for, and nothing on the way in explains it. A trigger already saying "all of them" keeps
            // saying it and firing that way.
            var asks = _asksAll && AsksAll;
            var changed = Resize(_blocks.Options, asks ? 2 : 0);
            if (!asks)
                return changed;

            changed |= Apply(_blocks.Options[0], "any one", !_all,
                Editor.Register(Cmd.RequireAny, delegate { SetAll(false); }));
            changed |= Apply(_blocks.Options[1], "all of them", _all,
                Editor.Register(Cmd.RequireAll, delegate { SetAll(true); }));

            return changed;
        }

        bool FillWhat()
        {
            _names.Clear();
            _ids.Clear();
            _forms.Clear();
            _groups.Clear();
            _waits.Clear();
            _sets.Clear();
            _ready.Clear();
            _why.Clear();

            if (_forEvents)
            {
                var events = Vocabulary.EventsOf(Editor.World, _selection);
                for (var i = 0; i < events.Count; i++)
                {
                    _names.Add(events[i].Text);
                    _ids.Add(events[i].Id);
                    _sets.Add(false);
                }
            }
            else
            {
                Grouped(Vocabulary.ActionsOf(Editor.World, _selection));
            }

            var note = Checked(Vocabulary.Least(Editor.World, _selection));
            if (!_forEvents && Vocabulary.Narrowed(Editor.World, _selection, Editor.ShowsTerminal))
                note = note.Length == 0 ? NarrowedSaid : note + " " + NarrowedSaid + ".";


            // A choice the blocks can no longer honour is dropped rather than left showing, so
            // "Done" never appears over a sentence that could not run.
            var chosen = _chosen;
            if (At(chosen, _chosenForm) < 0)
            {
                chosen = "";
                _chosen = "";
                _chosenForm = Form.Native;
            }

            var changed = false;
            if (_what.WaitsTip != WaitsSaid)
            {
                _what.WaitsTip = WaitsSaid;
                changed = true;
            }

            if (_what.Note != note)
            {
                _what.Note = note;
                changed = true;
            }

            if (_what.NarrowText != _whatSearch)
            {
                _what.NarrowText = _whatSearch;
                changed = true;
            }

            var narrow = Editor.RegisterText(Cmd.NarrowChoices, OnWhatSearch);
            if (_what.NarrowCommand != narrow)
            {
                _what.NarrowCommand = narrow;
                changed = true;
            }

            changed |= FillViews();

            _matched.Clear();
            for (var i = 0; i < _names.Count; i++)
            {
                // What is chosen stays listed whatever the search, as a chosen block does: a search
                // kept from an earlier picker must not hide the answer this one opened with.
                if (EntityQuery.MatchesText(_names[i], _whatSearch) || _ids[i] == _chosen)
                    _matched.Add(i);
            }

            // A heading before the first of each group that survived the search. Worked out here
            // rather than held in the list, because which groups have anything left in them is
            // decided by what somebody has just typed.
            _rows.Clear();
            var group = -1;
            for (var i = 0; i < _matched.Count; i++)
            {
                var at = _matched[i];
                // An event list is one list with no groups in it, so nothing is ever headed there.
                var mine = _groups.Count > at ? _groups[at] : group;
                if (_forEvents)
                    mine = group;

                if (mine != group)
                {
                    group = mine;
                    _rows.Add(-1 - mine);
                }

                _rows.Add(at);
            }

            changed |= Resize(_what.Entries, _rows.Count);
            for (var i = 0; i < _rows.Count; i++)
            {
                var at = _rows[i];

                var waits = at >= 0 && _waits.Count > at && _waits[at];
                if (_what.Entries[i].Waits != waits)
                {
                    _what.Entries[i].Waits = waits;
                    changed = true;
                }

                if (at < 0)
                {
                    if (_what.Entries[i].IsWithheld || _what.Entries[i].Tip != null)
                    {
                        _what.Entries[i].IsWithheld = false;
                        _what.Entries[i].Tip = null;
                        changed = true;
                    }

                    changed |= Apply(_what.Entries[i], Heading(-1 - at), "", "", false, false, 0, "", true);
                    continue;
                }

                var ready = _ready.Count > at ? _ready[at] : Readiness.Ready;
                var withheld = ready != Readiness.Ready;
                var tip = withheld ? _why[at] : null;
                if (_what.Entries[i].IsWithheld != withheld || _what.Entries[i].Tip != tip)
                {
                    _what.Entries[i].IsWithheld = withheld;
                    _what.Entries[i].Tip = tip;
                    changed = true;
                }

                // Something that cannot be made into a step is listed to be read about and not
                // chosen, so it is given nothing to do when pressed.
                var id = _ids[at];
                var form = _forms.Count > at ? _forms[at] : Form.Native;
                changed |= Apply(_what.Entries[i], _names[at], "", ReadinessText.Tag(ready), false,
                    id == chosen && form == _chosenForm,
                    withheld ? 0 : Editor.Register(Cmd.PickChoice, at, delegate { Choose(id, form); }));
            }

            return changed;
        }

        /// <summary>The two ways of looking at what a block offers, and which one is showing.</summary>
        bool FillViews()
        {
            if (_what.Views.Count == 0)
                return false;

            var changed = Apply(_what.Views[0], OrganizedSaid, !Editor.ShowsTerminal,
                Editor.Register(Cmd.ShowOrganized, delegate { Show(false); }));

            changed |= Apply(_what.Views[1], TerminalSaid, Editor.ShowsTerminal,
                Editor.Register(Cmd.ShowTerminal, delegate { Show(true); }));

            return changed;
        }

        void Show(bool terminal)
        {
            Editor.ShowsTerminal = terminal;
            Editor.Invalidate();
        }

        const string ValuesSaid = "Values";
        const string ActionsSaid = "Toolbar Actions";

        const string OrganizedSaid = "Organized";
        const string TerminalSaid = "Terminal";

        const string NarrowedSaid = "Currently showing only what the selected blocks share";

        /// <summary>
        /// What the list of what the blocks offer says above it, where one of them is not yet checked
        /// through.
        /// </summary>
        /// <remarks>
        /// Said of the least checked of them, since that is the one whose list may be wrong. Above the
        /// list rather than on it, because picking a block replaces the whole list anyway.
        /// </remarks>
        static string Checked(Evaluation least)
        {
            switch (least)
            {
                case Evaluation.Pending: return PendingSaid;
                case Evaluation.Partial: return PartialSaid;
                default: return "";
            }
        }

        const string PendingSaid = "Sprocket has not checked this block yet, so some of what it offers may "
            + "be doubled, missing or oddly named.";

        const string PartialSaid = "Sprocket has only partly checked this block, so some of what it offers "
            + "may still be doubled, missing or oddly named.";

        const string WaitsSaid = "This takes time to finish. The sequence waits until it is done before going on to the next step.";

        const int Actions = 0;
        const int Values = 1;

        /// <summary>
        /// Everything the blocks offer, values first and each group in its own order.
        /// </summary>
        /// <remarks>
        /// Sorted by the words rather than left in the order the game declared them, because a
        /// block hands its actions out in the order its code happens to create them and a person
        /// reading twenty of them is looking one up.
        /// <para>
        /// The headings are not in here. Only things somebody can choose are, so a search matches
        /// against those alone - a heading is put in front of a group at the moment the list is
        /// drawn, and only where that group still has something in it.
        /// </para>
        /// </remarks>
        void Grouped(List<EntityAction> actions)
        {
            actions.Sort(Editor.ShowsTerminal ? ByDeclared : BySaid);

            // The block's own division, which is the only one it declares.
            if (Editor.ShowsTerminal)
            {
                Listed(actions, Values);
                Listed(actions, Actions);
                return;
            }

            // Sprocket's, in the order the categories are declared, so a player who has learnt where
            // a light's colour sits finds a sorter's in the same place.
            for (var group = 0; group <= (int)Category.Other; group++)
                Listed(actions, group);
        }

        void Listed(List<EntityAction> actions, int group)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (!Lists(one) || Group(one) != group)
                    continue;

                _names.Add(Said(one));
                _ids.Add(one.Id);
                _forms.Add(one.Form);
                _groups.Add(group);
                _waits.Add(one.Lasts);
                _sets.Add(one.Sets);
                _ready.Add(one.Readiness);
                _why.Add(one.Why);
            }
        }

        /// <summary>What the view now showing calls this one.</summary>
        /// <remarks>
        /// The terminal view shows the block's own words as the block wrote them, and the organized one
        /// Sprocket's, where it has any.
        /// </remarks>
        string Said(EntityAction one)
        {
            if (Editor.ShowsTerminal)
                return one.Declared;

            // Values and actions are listed together here, so everything is said as something to do,
            // and a value as setting it: its sentence's own words, "set speed".
            var said = one.Sets ? one.Text : one.Named;
            if (one.Unaltered && !one.Renamed)
                said = Unlabelled(said);

            return string.IsNullOrEmpty(said) ? said : char.ToUpperInvariant(said[0]) + said.Substring(1);
        }

        /// <summary>The game's name for a member, without the colon it ends a terminal label with.</summary>
        /// <remarks>
        /// The game names some values the way its terminal labels the box beside them - "Mode:" - which
        /// in a list of things to choose reads as a label waiting for something after it. Only ever
        /// given the game's own names: a word Sprocket or a mod's definition chose is left as written,
        /// and the terminal view shows the game's as the game wrote them.
        /// </remarks>
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

        /// <summary>
        /// Where the thing on offer under that id is in the list, or -1 where it is not listed.
        /// </summary>
        /// <remarks>
        /// By the id and by which of the three ways it reaches the block, because the id alone can name
        /// more than one: a switch's value and the act that flips it share one.
        /// </remarks>
        /// <summary>
        /// Works the title out again where the answer has changed since it was last worked out.
        /// </summary>
        /// <remarks>
        /// Asked after every command and every tick, so a pick shows in the title in the frame it lands,
        /// and it compares before it builds: a tick where nothing was picked makes no string.
        /// </remarks>
        void Reword()
        {
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

            // A step acts on every block it names, so its blocks are always joined with "and". Only
            // what starts something can be any one of them, and then only where it was asked.
            var blocks = SentenceText.Entities(Editor.World, _selection, !_forEvents || _all);

            _said = _selection.Count == 0 && chosen.Length == 0
                ? _title
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

        int At(string id, Form form)
        {
            for (var i = 0; i < _ids.Count; i++)
            {
                if (_ids[i] == id && (_forms.Count > i ? _forms[i] : Form.Native) == form)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Whether the view now showing lists this one.
        /// </summary>
        /// <remarks>
        /// The terminal view lists what the block declares, whether or not something has been
        /// offered in its place; the organized view lists what is on offer, whoever worded it. A
        /// member nothing has touched is in both, under the one name it has.
        /// </remarks>
        bool Lists(EntityAction one)
        {
            if (Editor.ShowsTerminal)
                return one.Unaltered;

            // Something known to be out of reach has nothing to offer here, and is listed where the
            // block is shown as it is, saying so.
            return !one.Hidden && one.Readiness != Readiness.Unsupported;
        }

        /// <summary>
        /// Which group of the view now showing this one is listed in.
        /// </summary>
        /// <remarks>
        /// Two sets of numbers through one field, because only one view is ever on screen: the
        /// terminal's are its two kinds of member, and the organized one's are the categories. What
        /// a number means is settled by which view asked, which is also what <see cref="Heading" />
        /// needs to word it.
        /// </remarks>
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
                return group == Values ? ValuesSaid : ActionsSaid;

            return CategoryText.Of((Category)group);
        }

        readonly List<Form> _forms = new List<Form>();

        readonly List<int> _groups = new List<int>();

        readonly List<bool> _waits = new List<bool>();

        readonly List<bool> _sets = new List<bool>();

        readonly List<Readiness> _ready = new List<Readiness>();

        readonly List<string> _why = new List<string>();

        readonly List<int> _rows = new List<int>();

        static readonly SaidOrder BySaid = new SaidOrder(false);

        static readonly SaidOrder ByDeclared = new SaidOrder(true);

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

        /// <summary>Writes what a row says and does onto <paramref name="entry" />, and answers whether any of it changed.</summary>
        internal static bool Apply(PickOption entry, string text, string secondary, string status,
            bool active, bool chosen, int command, string icon = "", bool heading = false)
        {
            var changed = false;

            if (entry.IsHeading != heading) { entry.IsHeading = heading; changed = true; }

            if (entry.Icon != icon) { entry.Icon = icon; changed = true; }

            if (entry.Text != text) { entry.Text = text; changed = true; }
            if (entry.Secondary != secondary) { entry.Secondary = secondary; changed = true; }
            if (entry.Status != status) { entry.Status = status; changed = true; }
            if (entry.IsActive != active) { entry.IsActive = active; changed = true; }
            if (entry.IsChosen != chosen) { entry.IsChosen = chosen; changed = true; }
            if (entry.Command != command) { entry.Command = command; changed = true; }

            return changed;
        }

        static bool Apply(KindFilter filter, string label, int count, bool current, int command)
        {
            var changed = false;

            if (filter.Label != label) { filter.Label = label; changed = true; }
            if (filter.Count != count) { filter.Count = count; changed = true; }
            if (filter.IsCurrent != current) { filter.IsCurrent = current; changed = true; }
            if (filter.Command != command) { filter.Command = command; changed = true; }

            return changed;
        }

        /// <summary>Writes what a choice says and does onto <paramref name="option" />, and answers whether any of it changed.</summary>
        internal static bool Apply(ChoiceEntry option, string text, bool current, int command)
        {
            var changed = false;

            if (option.Text != text) { option.Text = text; changed = true; }
            if (option.IsCurrent != current) { option.IsCurrent = current; changed = true; }
            if (option.Command != command) { option.Command = command; changed = true; }

            return changed;
        }

        /// <summary>Makes <paramref name="list" /> hold <paramref name="count" /> rows, and answers whether it had to.</summary>
        internal static bool Resize(List<PickOption> list, int count)
        {
            var changed = false;
            while (list.Count < count) { list.Add(new PickOption("", 0)); changed = true; }
            while (list.Count > count) { list.RemoveAt(list.Count - 1); changed = true; }
            return changed;
        }

        static bool Resize(List<KindFilter> list, int count)
        {
            var changed = false;
            while (list.Count < count) { list.Add(new KindFilter("", 0, false, 0)); changed = true; }
            while (list.Count > count) { list.RemoveAt(list.Count - 1); changed = true; }
            return changed;
        }

        /// <summary>Makes <paramref name="list" /> hold <paramref name="count" /> choices, and answers whether it had to.</summary>
        internal static bool Resize(List<ChoiceEntry> list, int count)
        {
            var changed = false;
            while (list.Count < count) { list.Add(new ChoiceEntry("", false, 0)); changed = true; }
            while (list.Count > count) { list.RemoveAt(list.Count - 1); changed = true; }
            return changed;
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

        /// <summary>
        /// Works out what is on offer, which does not depend on what has been picked.
        /// </summary>
        /// <remarks>
        /// The ship is the ship. Ticking a block narrows what can be said about it in the
        /// next stage and nothing else, so nobody ever watches the list of their own blocks
        /// shrink underneath them.
        ///
        /// Narrowing is the one thing that can hide a row, and a picked block is exempt: a
        /// choice has to be undoable from where it was made, and a tick nobody can find is a
        /// tick nobody can clear.
        ///
        /// Sorted by name, and not by the order the world hands them over. That order is whatever
        /// walking the grid produced, which means nothing to somebody looking for a door. Picking
        /// still moves nothing, because the sort does not know what is selected.
        /// </remarks>
        void Collect()
        {
            _candidates.Clear();

            var capable = Vocabulary.Capable(Editor.World, _forEvents);
            for (var i = 0; i < capable.Count; i++)
                _candidates.Add(capable[i]);

            _candidates.Sort(ByName);

            _kinds.Clear();
            EntityQuery.KindsPresent(_candidates, _kinds);

            _filtered.Clear();
            for (var i = 0; i < _candidates.Count; i++)
            {
                var entity = _candidates[i];
                if (Contains(_selection, entity.Id)
                    || EntityQuery.Matches(entity, _blockSearch, _kind))
                    _filtered.Add(entity);
            }
        }

        /// <summary>
        /// Takes a block, the way the press said it was taken.
        /// </summary>
        /// <remarks>
        /// Three acts and not three commands, because they are one act with a qualifier: a row was
        /// chosen, and the qualifier says what happens to everything else. Which key said so is the
        /// screen's business and never reaches here.
        /// </remarks>
        void Pick(long entityId, Held how)
        {
            if (how == Held.Through && _anchor != 0L)
            {
                Span(_anchor, entityId);
                Editor.Invalidate();
                return;
            }

            // The anchor is where a run would start from, so it follows the presses that take one
            // block deliberately and not the ones that take a run. Shift with nothing to reach back
            // to is the first press of a run rather than an error.
            _anchor = entityId;

            if (how == Held.Also)
                Toggle(entityId);
            else
                Only(entityId);

            // What is on offer changes the moment something is picked, so the whole
            // path is rebuilt rather than left stale.
            Editor.Invalidate();
        }

        long _anchor;

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

        /// <summary>
        /// Everything between two blocks, in the order they are listed.
        /// </summary>
        /// <remarks>
        /// The listed order and not the order they were picked, because a run is what somebody drew
        /// down the screen. A block that has been filtered out of the list since the anchor was set
        /// is not between anything, so a run that cannot be found leaves the selection alone.
        /// </remarks>
        void Span(long from, long to)
        {
            int first = Listed(from);
            int last = Listed(to);

            if (first < 0 || last < 0)
                return;

            if (first > last)
            {
                int swap = first;
                first = last;
                last = swap;
            }

            _selection.Clear();
            for (int i = first; i <= last; i++)
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
            Editor.Invalidate();
        }

        void SetAll(bool all)
        {
            _all = all;
            Editor.Invalidate();
        }

        /// <summary>
        /// Takes new search text, and ignores text that is not new.
        /// </summary>
        /// <remarks>
        /// A target that puts this page's own value back into its search box, and reports that
        /// as an edit, must not cost a rebuild. Some raise the change notification
        /// after the assignment has returned, so the target cannot always tell its own
        /// echo apart from a keystroke - and a rebuild would rewrite the box, which
        /// echoes again. This is where that has to stop, because every target would
        /// otherwise have to know not to start it.
        /// </remarks>
        void OnBlockSearch(string value)
        {
            value = value ?? "";
            if (value == _blockSearch)
                return;

            _blockSearch = value;
            _filter.Blocks = value;
            Editor.Invalidate();
        }

        void OnWhatSearch(string value)
        {
            value = value ?? "";
            if (value == _whatSearch)
                return;

            _whatSearch = value;
            _filter.Choices = value;
            Editor.Invalidate();
        }

        void ClearKind()
        {
            SetKind("");
        }

        void SetKind(string kindId)
        {
            _kind = kindId;

            // Kept where it outlives this page, so the next picker of this sort opens the way this
            // one was left - and no other sort is touched by it.
            _filter.Kind = kindId ?? "";
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
    }
}
