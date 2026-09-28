using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Values;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Localization;

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
    sealed partial class PickPage : EditorPage
    {
        const bool AsksAll = false;
        const int Looked = 15;
        const int Actions = 0;
        const int Values = 1;

        static readonly NameOrder ByName = new NameOrder();
        static readonly SaidOrder BySaid = new SaidOrder(false);
        static readonly SaidOrder ByDeclared = new SaidOrder(true);

        readonly string _title;
        readonly List<long> _selection = new List<long>();
        readonly bool _forEvents;
        readonly bool _asksAll;
        readonly Action<PickAnswer> _done;
        readonly List<GridEntity> _candidates = new List<GridEntity>();
        readonly List<GridEntity> _filtered = new List<GridEntity>();
        readonly List<long> _filteredIds = new List<long>();
        readonly List<EntityKind> _kinds = new List<EntityKind>();
        readonly Filter _filter;
        readonly List<EntityKind> _shownKinds = new List<EntityKind>();
        readonly List<string> _kindKeys = new List<string>();
        readonly List<string> _names = new List<string>();
        readonly List<string> _ids = new List<string>();
        readonly List<int> _matched = new List<int>();
        readonly List<Form> _forms = new List<Form>();
        readonly List<int> _groups = new List<int>();
        readonly List<bool> _waits = new List<bool>();
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
        readonly List<Field> _givenFields = new List<Field>();
        readonly Func<GridEntity, bool> _fits;

        string _chosen;
        Form _chosenForm;
        bool _all;
        string _blockSearch;
        string _kind;
        string _kindSearch;
        string _whatSearch;
        string _said;
        string _saidChosen = "";
        int _saidOffered;
        bool _saidAll;
        bool _retitled;
        bool _goesOn;
        bool _straight;
        string _entered;
        string _given;
        bool _openedValues;
        long _anchor;
        Interval _looked = new Interval(Looked);

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
        ICommand _organized;
        ICommand _terminal;
        ICommand _finish;

        /// <summary>Asks what to point at and what should happen to it, starting from whatever the answer is now.</summary>
        /// <remarks>
        /// <paramref name="after" /> is the strip of the pick this page goes on in, as its next page, or null for a pick of its
        /// own. Opened the way the last picker of its sort was left: what is on this construct is not known yet, so a kind it
        /// does not have is dropped later, where it is.
        /// </remarks>
        public PickPage(SprocketEditor editor, string title, bool forEvents,
            IList<long> targets, string chosen, Form chosenForm, Action<PickAnswer> done,
            bool asksAll = false, bool all = false, IList<Literal> values = null,
            Opening opens = Opening.Blocks, Aim aim = null, PickStrip after = null)
            : base(editor)
        {
            _opens = opens;
            _aim = aim;
            if (aim != null)
            {
                aim.Onward = Primary;
                aim.MovesOn = MoveOn;
            }

            _strip = after ?? new PickStrip(editor.Texts, delegate { return Editor.Windows; });
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

        /// <summary>Asks which blocks fill a slot: the blocks alone, only those <paramref name="fits" /> allows, starting from <paramref name="targets" />.</summary>
        /// <remarks>
        /// Narrowed afresh each time rather than the way the last pick was left, since the blocks a slot is offered are already
        /// only the ones it can take, and a kind remembered from choosing what a step does would hide them.
        /// </remarks>
        public PickPage(SprocketEditor editor, string title, IList<long> targets, Func<GridEntity, bool> fits,
            Action<PickAnswer> done)
            : this(editor, title, false, targets, "", Form.Native, done)
        {
            _fits = fits;
            _filter = new Filter();
            _kind = "";
            _blockSearch = "";
            _kindSearch = "";
            _whatSearch = "";
        }

        /// <summary>The title as the answer stands, or the picker's own name where nothing is chosen.</summary>
        public override string Title => _said ?? _title;

        /// <inheritdoc />
        /// <remarks>Nothing where what the trigger does goes on to a page of its own.</remarks>
        public override string PrimaryText => IsComplete && (_aim == null || _aim.Finishes) ? Editor.Texts.PickDone : "";

        /// <summary>What the chosen action will not do anything without, which the column of values asks for beside the stages.</summary>
        /// <remarks>
        /// Nearly nothing wants values, so this is empty nearly always. When it is not, the count is the whole of what can
        /// honestly be asked: the game declares how many values an action takes and nothing about what any of them is for.
        /// </remarks>
        public string Question
        {
            get
            {
                var fields = Wanted();
                return fields.Count > 0 ? SequenceText.Wants(fields) : "";
            }
        }

        /// <summary>What each value the chosen action takes wants, one field each, and empty where it takes none.</summary>
        public IList<Field> Fields => Wanted();

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
            var values = _strip.Values;
            if (_forEvents || !_strip.ShowsValues || values == null || values.Form.Count == 0)
            {
                _done(new PickAnswer(_selection, _chosen, _all, null, _chosenForm));
                return;
            }

            var form = values.Form;
            form.Judge();
            values.Why = form.Wrong;
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
            var values = new List<Literal>();
            ValueLine.Read(said, values);
            _done(new PickAnswer(_selection, _chosen, _all, values, _chosenForm));
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

            // A slot's pick is finished from the only column it has.
            if (_fits != null)
            {
                // ReSharper disable once PossibleNullReferenceException - made above where it had not been, by MakeBlocks.
                _blocks.Finishes = true;
                _blocks.Done.Command = _finish;
                _blocks.Done.IsEnabled = PrimaryText.Length != 0;
            }

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
                _does = new StageColumn(_strip.Width, false, null, Editor.Texts, Windows);
                _does.Title = Editor.Texts.PickWhatItDoes;
                _does.Done.Command = _finish;
                _strip.Add(_does);
            }
            else if (!does && _does != null)
            {
                _strip.RemoveLast();
                _does = null;
            }

            FillGiven();

            if (_does != null && _aim != null)
            {
                _aim.Fill(_does);
                _does.Done.IsEnabled = PrimaryText.Length != 0;
            }

            _strip.Show();
            _strip.Arrive(Opens, Editor.Input);

            if (!_goesOn)
                return;

            _goesOn = false;
            Primary();
        }

        /// <summary>Brings what the blocks are doing up to date, on a cadence, and moves the strip's page turning on a tick.</summary>
        /// <remarks>
        /// On a cadence, because nothing in the game announces a door opening or a vent filling: this is the one thing here
        /// that has to be asked for.
        /// </remarks>
        public override void Refresh()
        {
            _strip.Tick();

            if (_blocks == null || !_looked.Due(Editor.CurrentTick))
                return;

            for (var i = 0; i < _filtered.Count; i++)
            {
                var entity = Editor.World.FindEntity(_filtered[i].Id);
                var row = _blockRows[_filtered[i].Id];
                if (entity == null || row == null || row.Status == entity.Status)
                    continue;

                row.Status = entity.Status;
                row.Show();
            }
        }

        /// <summary>Goes straight on to the page after this one as soon as this is built, for a part pressed that stands for something on that page.</summary>
        /// <remarks>Built first all the same, because the page after carries on from the stages this one asks.</remarks>
        internal void GoOn()
        {
            _goesOn = true;
        }

        /// <summary>Says whether this page is being opened straight onto, rather than paged to.</summary>
        internal void Arrived(bool straight)
        {
            _straight = straight;
        }

        /// <inheritdoc />
        /// <remarks>A picker arriving on screen reads what the blocks are doing now rather than what they were doing when it was last looked at.</remarks>
        internal override void Resay()
        {
            base.Resay();
            _looked.Restart();
        }

        /// <summary>Gives the keyboard to the box of the column the pick opens on, now that there is a screen for it.</summary>
        protected override void ViewCreated()
        {
            _strip.Arrive(Opens, Editor.Input);
        }

        Ui.Windows.WindowLayer Windows()
        {
            return Editor.Windows;
        }

        void MakeBlocks()
        {
            _blocks = new StageColumn(_strip.Width, true, null, Editor.Texts, Windows);
            _blocks.Title = Editor.Texts.PickBlocks;
            _blocks.EmptyText = _fits != null
                ? Editor.Texts.BlueprintNoneFits
                : _forEvents ? Editor.Texts.PickNoBlocksHappen : Editor.Texts.PickNoBlocksDone;

            // For the whole life of the column, so the line does not come and go as the list of kinds is narrowed.
            _blocks.HasKinds = _kinds.Count > 0;
            _blocks.Narrowed += OnBlockSearch;
            _blocks.KindsNarrowed += NarrowKinds;

            _blockRows = new Keyed<long, PickRow>(_blocks.Rows, BlockRow);
            _kindRows = new Keyed<string, KindRow>(_blocks.Kinds, KindRow);
            _clearKind = Editor.Act(Cmd.AnyKind, delegate { SetKind(""); });

            _strip.Add(_blocks);
        }

        void MakeWhat()
        {
            // Two ways of looking only at what somebody does to a block. What happens to one is Sprocket's own wording
            // throughout, so there is no second way to look at it.
            var views = _forEvents ? null : new[] { Editor.Texts.PickOrganized, Editor.Texts.PickTerminal };

            _what = new StageColumn(_strip.Width, true, views, Editor.Texts, Windows);
            _what.Title = _forEvents ? Editor.Texts.PickWhatHappens : Editor.Texts.PickWhatToDo;
            _what.EmptyText = _forEvents ? Editor.Texts.PickNothingHappens : Editor.Texts.PickNothingDone;
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
            return new KindRow
            {
                Command = Editor.Act(kind.Length == 0 ? Cmd.AnyKind : Cmd.OneKind, delegate
                {
                    _blocks.CloseKinds();
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
            if (PrimaryText.Length == 0)
                return;

            var values = _strip.Values;
            if (_strip.ShowsValues && values != null && values.Form.Count > 0 && !values.Form.Complete)
            {
                values.Form.Take(Editor.Input);
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
            if (_forEvents)
            {
                _strip.ShowsValues = false;
                return;
            }

            var given = IsComplete ? _chosenForm + " " + _chosen : "";
            _strip.ShowsValues = _what != null && given.Length != 0;

            var fields = Wanted();
            if (_given == given && Same(_givenFields, fields))
                return;

            _given = given;
            _givenFields.Clear();
            _givenFields.AddRange(fields);

            // Read once, where a color is asked for: these are the colors this player paints with, only ever read.
            var colors = new List<Hsv>();
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Edits != Features.Values.Editor.Colour)
                    continue;

                Player.Colours(colors);
                break;
            }

            var made = _strip.Values == null;
            var values = _strip.Restate(given, fields, Filled, colors);
            if (made)
            {
                values.Form.Edited += delegate
                {
                    if (Editor.Current != this)
                        return;

                    _entered = values.Form.Answer;
                    values.Why = values.Form.Wrong;
                };

                values.Form.Submitted += delegate
                {
                    if (Editor.Current == this && _finish != null)
                        _finish.Execute();
                };
            }

            values.Done.Command = _finish;
            _entered = values.Form.Answer;

            // Nowhere to type is nowhere to keep the keyboard. Somebody who came for the values gets it in them, once.
            if (_opens == Opening.Values && !_openedValues && values.Form.Count > 0)
            {
                _openedValues = true;
                values.Form.Take(Editor.Input);
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
            _blocks.NarrowText = _blockSearch;

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

                row.Text = entity.Name;
                row.Secondary = entity.Kind.DisplayName;
                row.Status = entity.Status;
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
            // A kind remembered from an earlier picker may not be on this construct at all, and one that is not would hide
            // every block with nothing on screen to say why.
            if (_kind.Length > 0 && !HasKind(_kind))
                SetKind("");

            _shownKinds.Clear();
            for (var i = 0; i < _kinds.Count; i++)
            {
                // The one in force is always offered, however the box is narrowed, so what the line says is showing is
                // always something the list can be seen to say too.
                if (_kinds[i].Id == _kind || EntityQuery.MatchesText(_kinds[i].DisplayName, _kindSearch))
                    _shownKinds.Add(_kinds[i]);
            }

            _kindKeys.Clear();
            _kindKeys.Add("");
            for (var i = 0; i < _shownKinds.Count; i++)
                _kindKeys.Add(_shownKinds[i].Id);

            _kindRows.Show(_kindKeys);

            var anything = _kindRows[""];
            anything.Text = Editor.Texts.PickAnything;
            anything.Count = _candidates.Count;
            anything.IsCurrent = _kind.Length == 0;

            var current = Editor.Texts.PickAnything;
            for (var i = 0; i < _shownKinds.Count; i++)
            {
                var row = _kindRows[_shownKinds[i].Id];
                row.Text = _shownKinds[i].DisplayName;
                row.Count = CountOfKind(_shownKinds[i].Id);
                row.IsCurrent = _kind == _shownKinds[i].Id;
                if (row.IsCurrent)
                    current = _shownKinds[i].DisplayName;
            }

            _blocks.Kind = current;
            _blocks.KindText = _kindSearch;
            _blocks.ClearKind = _kind.Length > 0 ? _clearKind : null;
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
                _filter.Kinds = said;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>Offered wherever the distinction between any one block and all of them means anything, which is nowhere yet.</summary>
        /// <remarks>
        /// A trigger that waits for all of its blocks is built and tested, but the choice would be offered on triggers it means
        /// nothing for, and nothing on the way in explains it. A trigger already saying "all of them" keeps saying it and
        /// firing that way.
        /// </remarks>
        void FillOptions()
        {
            var asks = _asksAll && AsksAll;
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

            var texts = Editor.Texts;
            var note = Checked(texts, Vocabulary.Least(Editor.World, _selection));
            if (!_forEvents && Vocabulary.Narrowed(Editor.World, _selection, Editor.ShowsTerminal))
                note = note.Length == 0 ? texts.PickShared : note + " " + texts.PickShared + ".";

            // A choice the blocks can no longer honor is dropped rather than left showing, so Done never appears over a
            // sentence that could not run.
            var chosen = _chosen;
            if (At(chosen, _chosenForm) < 0)
            {
                chosen = "";
                _chosen = "";
                _chosenForm = Form.Native;
            }

            _what.Note = note;
            _what.NarrowText = _whatSearch;
            _what.ShowViews(Editor.ShowsTerminal ? 1 : 0, new[] { _organized, _terminal });

            _matched.Clear();
            for (var i = 0; i < _names.Count; i++)
            {
                // What is chosen stays listed whatever the search, as a chosen block does: a search kept from an earlier
                // picker must not hide the answer this one opened with.
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
                row.WaitsTip = texts.PickWaits;

                if (at < 0)
                {
                    row.Text = Heading(-1 - at);
                    row.IsHeading = true;
                    row.IsWithheld = false;
                    row.IsChosen = false;
                    row.Waits = false;
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
                row.Status = ReadinessText.Tag(ready);
                row.IsChosen = _ids[at] == chosen && form == _chosenForm;
                row.Waits = _waits.Count > at && _waits[at];
                row.Show();
            }

            _what.ShowEmpty();
        }

        void Show(bool terminal)
        {
            Editor.ShowsTerminal = terminal;
            Editor.Invalidate();
        }

        /// <summary>What the list of what the blocks offer says above it, where one of them is not yet checked through.</summary>
        /// <remarks>Said of the least checked of them, since that is the one whose list may be wrong.</remarks>
        static string Checked(Texts texts, Evaluation least)
        {
            switch (least)
            {
                case Evaluation.Pending: return texts.PickPending;
                case Evaluation.Partial: return texts.PickPartial;
                default: return "";
            }
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
                _waits.Add(one.Lasts);
                _sets.Add(one.Sets);
                _ready.Add(one.Readiness);
                _why.Add(one.Why);
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
        /// organized view lists what is on offer, whoever worded it, and nothing known to be out of reach.
        /// </remarks>
        bool Lists(EntityAction one)
        {
            if (Editor.ShowsTerminal)
                return one.Unaltered;

            return !one.Hidden && one.Readiness != Readiness.Unsupported;
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

            return CategoryText.Of((Category)group);
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
        /// exempt: a tick nobody can find is a tick nobody can clear. Sorted by name, because the order the world hands them
        /// over is whatever walking the grid produced.
        /// </remarks>
        void Collect()
        {
            _candidates.Clear();

            if (_fits != null)
            {
                var entities = Editor.World.Entities;
                for (var i = 0; i < entities.Count; i++)
                {
                    if (_fits(entities[i]))
                        _candidates.Add(entities[i]);
                }
            }
            else
            {
                var capable = Vocabulary.Capable(Editor.World, _forEvents);
                for (var i = 0; i < capable.Count; i++)
                    _candidates.Add(capable[i]);
            }

            _candidates.Sort(ByName);

            _kinds.Clear();
            EntityQuery.KindsPresent(_candidates, _kinds);

            _filtered.Clear();
            _filteredIds.Clear();
            for (var i = 0; i < _candidates.Count; i++)
            {
                var entity = _candidates[i];
                if (!Contains(_selection, entity.Id) && !EntityQuery.Matches(entity, _blockSearch, _kind))
                    continue;

                _filtered.Add(entity);
                _filteredIds.Add(entity.Id);
            }
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
            if (value == _blockSearch)
                return;

            var narrow = Editor.Act(Cmd.NarrowEntities, delegate
            {
                _blockSearch = value;
                _filter.Blocks = value;
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
                _filter.Choices = value;
                Editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>Narrows the blocks to one kind, kept where it outlives this page so the next picker of its sort opens the same way.</summary>
        void SetKind(string kindId)
        {
            _kind = kindId ?? "";
            _filter.Kind = _kind;
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

        /// <remarks>A comparer object rather than a <c>Comparison</c> delegate, which Space Engineers does not whitelist.</remarks>
        sealed class NameOrder : IComparer<GridEntity>
        {
            public int Compare(GridEntity a, GridEntity b)
            {
                // Neither can be null: the only lists sorted with this are the world's own entities.
                // ReSharper disable PossibleNullReferenceException
                var byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.Id.CompareTo(b.Id);
                // ReSharper restore PossibleNullReferenceException
            }
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
