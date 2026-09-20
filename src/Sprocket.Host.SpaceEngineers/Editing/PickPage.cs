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
        bool _all;

        readonly List<GridEntity> _candidates = new List<GridEntity>();
        readonly List<GridEntity> _filtered = new List<GridEntity>();

        /// <summary>
        /// A small number per entity, handed out in the order they are first seen and kept for the life of
        /// this page.
        /// </summary>
        /// <remarks>
        /// A command's number is twelve bits, and an entity's id is whatever the game hands us. Space
        /// Engineers gives blocks ids in the quintillions, so casting one into that field clamped every
        /// block to the same number and the second one registered threw. The invented ship numbers its
        /// blocks from zero, which is why this only ever appeared against a real one.
        ///
        /// A row's position would fit and would be wrong: narrowing the list, or a block being welded on,
        /// would leave a held id pointing at a different block. That is the "which slot was it in" bug the
        /// whole command redesign existed to kill, so the map is by identity and outlives any rebuild.
        /// </remarks>
        readonly Dictionary<long, int> _slots = new Dictionary<long, int>();

        /// <summary>
        /// Name first, then id.
        /// </summary>
        /// <remarks>
        /// Ordinal rather than the player's culture, because the same ship has to list in the same order
        /// wherever it is opened. The id breaks ties, and there are always ties: a ship carries a dozen
        /// blocks called "Interior Light" and they must not swap places between one rebuild and the next.
        /// </remarks>
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

        string _blockSearch = "";
        string _kind = "";
        string _whatSearch = "";
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
            IList<long> targets, string chosen, Action<PickAnswer> done,
            bool asksAll = false, bool all = false)
            : base(editor)
        {
            _title = title;
            _forEvents = forEvents;
            _chosen = chosen ?? "";
            _asksAll = asksAll;
            _all = all;
            _done = done;

            for (var i = 0; i < targets.Count; i++)
                _selection.Add(targets[i]);
        }

        public override string Title => _title;

        public override string Subtitle
        {
            get
            {
                if (_selection.Count == 0)
                    return "Start by picking one or more blocks";
                return IsComplete ? "" : "Now pick what happens";
            }
        }

        public override string PrimaryText => IsComplete ? "Done" : "";

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

        public override void Primary()
        {
            _done(new PickAnswer(_selection, _chosen, _all));
        }

        public override void Primary(string said)
        {
            var values = new List<Literal>();
            ValueLine.Read(said, values);
            _done(new PickAnswer(_selection, _chosen, _all, values));
        }

        List<Field> Fields()
        {
            return IsComplete ? Editor.Fields(_selection, _chosen) : _nothing;
        }

        readonly List<Field> _nothing = new List<Field>();

        bool IsComplete => _selection.Count > 0 && !string.IsNullOrEmpty(_chosen);

        public override void Build(List<EditorComponent> components)
        {
            Collect();

            if (_pick == null)
            {
                _pick = new PickComponent();
                _pick.Fills = true;

                _blocks = new PickStage("Blocks");
                _blocks.AllowsMany = true;
                _blocks.EmptyText = _forEvents
                    ? "Nothing here has anything that can happen to it."
                    : "Nothing here has anything that can be done to it.";
                _pick.Add(_blocks);
            }

            var changed = FillBlocks();

            // The second stage exists only once there is something for it to be about.
            var wanted = _selection.Count > 0;
            if (wanted && _what == null)
            {
                _what = new PickStage(_forEvents ? "What happens" : "What to do");
                _what.EmptyText = _forEvents
                    ? "Those blocks have nothing in common that can happen to them."
                    : "Those blocks have nothing in common that can be done to them.";
                _pick.Add(_what);
            }
            else if (!wanted && _what != null)
            {
                _pick.RemoveLast();
                _what = null;
            }

            if (_what != null)
                changed |= FillWhat();

            if (changed)
                _pick.Changed();

            components.Add(_pick);
        }

        /// <summary>How often the ship is asked what its blocks are doing, in ticks.</summary>
        /// <remarks>
        /// Four times a second. A door opening is worth showing promptly and is not worth asking the
        /// game about sixty times a second: on a construct of forty blocks that is a hundred and sixty
        /// questions a tick, for a list whose fastest row takes the better part of a second to change.
        /// </remarks>
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
                    Editor.Register(Cmd.PickEntity, Slot(id), delegate { Toggle(id); }),
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
            var changed = Resize(_blocks.Filters, _kinds.Count + 1);

            changed |= Apply(_blocks.Filters[0], "Anything", _candidates.Count, _kind.Length == 0,
                Editor.Register(Cmd.AnyKind, ClearKind));

            for (var i = 0; i < _kinds.Count; i++)
            {
                var id = _kinds[i].Id;
                changed |= Apply(_blocks.Filters[i + 1], _kinds[i].DisplayName, CountOfKind(id),
                    _kind == id, Editor.Register(Cmd.OneKind, i, delegate { SetKind(id); }));
            }

            return changed;
        }

        /// <summary>
        /// Whether the choice between any one of the blocks and all of them is put on screen.
        /// </summary>
        /// <remarks>
        /// <b>Off on purpose, and not because nothing is behind it.</b> A trigger that waits for all
        /// of its blocks is built, sampled and tested - it fires as the last one arrives, and its
        /// sentence joins the blocks with "and" rather than "or". What is not there is the rest of
        /// what makes it usable: it is offered on every trigger, including the ones it cannot mean
        /// anything for, and nothing on the way in explains the difference it is asking about.
        /// <para>
        /// One line here rather than at the four places that open a picker, so it comes back in one
        /// line too. Nothing else changes: a trigger that already says "all of them" keeps saying it
        /// and keeps firing that way.
        /// </para>
        /// </remarks>
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
            _values.Clear();

            if (_forEvents)
            {
                var events = Vocabulary.EventsOf(Editor.World, _selection);
                for (var i = 0; i < events.Count; i++)
                {
                    _names.Add(events[i].Text);
                    _ids.Add(events[i].Id);
                }
            }
            else
            {
                Grouped(Vocabulary.ActionsOf(Editor.World, _selection));
            }

            // A choice the blocks can no longer honour is dropped rather than left showing, so
            // "Done" never appears over a sentence that could not run.
            var chosen = _chosen;
            if (!_ids.Contains(chosen))
            {
                chosen = "";
                _chosen = "";
            }

            var changed = false;
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

            _matched.Clear();
            for (var i = 0; i < _names.Count; i++)
            {
                if (EntityQuery.MatchesText(_names[i], _whatSearch))
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
                var mine = _values.Count > at && _values[at] ? 1 : 0;
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
                if (at < 0)
                {
                    changed |= Apply(_what.Entries[i], at == -2 ? ValuesSaid : ActionsSaid,
                        "", "", false, false, 0, "", true);

                    continue;
                }

                var id = _ids[at];
                changed |= Apply(_what.Entries[i], _names[at], "", "", false, id == chosen,
                    Editor.Register(Cmd.PickChoice, at, delegate { Choose(id); }));
            }

            return changed;
        }

        /// <summary>The heading over the block's own values, and the one over its actions.</summary>
        /// <remarks>
        /// Named for where somebody has already met them. Almost nobody arrives here without having
        /// assigned a toolbar action by hand, so that is the half of the list that needs no
        /// introduction - which is why it is the half that sits lower down.
        /// </remarks>
        const string ValuesSaid = "Values";
        const string ActionsSaid = "Toolbar Actions";

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
            actions.Sort(BySaid);

            Listed(actions, true);
            Listed(actions, false);
        }

        void Listed(List<EntityAction> actions, bool values)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                EntityAction one = actions[i];
                if (one.GamePropertyId != null != values)
                    continue;

                _names.Add(one.Named);
                _ids.Add(one.Id);
                _values.Add(values);
            }
        }

        /// <summary>Which group each thing on offer belongs to, one per entry in <c>_names</c>.</summary>
        readonly List<bool> _values = new List<bool>();

        /// <summary>
        /// What to draw, in order: an index into the things on offer, or a heading.
        /// </summary>
        /// <remarks>
        /// A heading is a negative, which is the cheapest way to say "not one of them" in a list of
        /// positions - minus two for the values, minus one for the actions.
        /// </remarks>
        readonly List<int> _rows = new List<int>();

        /// <summary>By the words, because that is what somebody is scanning the list for.</summary>
        static readonly SaidOrder BySaid = new SaidOrder();

        sealed class SaidOrder : IComparer<EntityAction>
        {
            public int Compare(EntityAction left, EntityAction right)
            {
                return string.Compare(Said(left), Said(right), StringComparison.OrdinalIgnoreCase);
            }

            static string Said(EntityAction one)
            {
                return one == null ? "" : one.Named;
            }
        }

        static bool Apply(PickOption entry, string text, string secondary, string status,
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

        static bool Apply(ChoiceEntry option, string text, bool current, int command)
        {
            var changed = false;

            if (option.Text != text) { option.Text = text; changed = true; }
            if (option.IsCurrent != current) { option.IsCurrent = current; changed = true; }
            if (option.Command != command) { option.Command = command; changed = true; }

            return changed;
        }

        static bool Resize(List<PickOption> list, int count)
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

        static bool Resize(List<ChoiceEntry> list, int count)
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
        /// choice has to be undoable from where it was made, and a tick you cannot find is a
        /// tick you cannot clear.
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

        void Toggle(long entityId)
        {
            if (Contains(_selection, entityId))
                _selection.Remove(entityId);
            else
                _selection.Add(entityId);

            // What is on offer changes the moment something is picked, so the whole
            // path is rebuilt rather than left stale.
            Editor.Invalidate();
        }

        void Choose(string id)
        {
            _chosen = id;
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
        /// A target that puts our own value back into its search box, and reports that
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
            Editor.Invalidate();
        }

        void OnWhatSearch(string value)
        {
            value = value ?? "";
            if (value == _whatSearch)
                return;

            _whatSearch = value;
            Editor.Invalidate();
        }

        void ClearKind()
        {
            SetKind("");
        }

        void SetKind(string kindId)
        {
            _kind = kindId;
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
