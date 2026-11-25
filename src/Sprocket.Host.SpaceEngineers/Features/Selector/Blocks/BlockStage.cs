using Sprocket.Host.SpaceEngineers.Features.Selector.Members;
using Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Blocks
{
    /// <summary>
    /// The first stage of a pick: the blocks listed to be picked, or the slots of a blueprint to be picked by name, with the
    /// search, the kinds they are narrowed to, the hidden blocks and how several are read.
    /// </summary>
    public class BlockStage
    {
        // The ways an expression combines the values of several blocks it reads.
        static readonly IReadOnlyList<Quantifier> Combinings = new[] { Quantifier.Sum, Quantifier.Average, Quantifier.Min, Quantifier.Max };

        // The ways a condition an expression builds reads several blocks: combined, or a comparison checked on any or all of them.
        static readonly IReadOnlyList<Quantifier> Checkings = new[] { Quantifier.Sum, Quantifier.Average, Quantifier.Min, Quantifier.Max, Quantifier.Any, Quantifier.All };

        readonly SprocketEditor _editor;
        readonly PickChoices _picked;
        readonly EditorPage _screen;
        readonly BlockListing _listing;
        readonly KindFilter _kinds = new KindFilter();
        readonly bool _forEvents;
        readonly bool _forTests;
        readonly bool _forReadings;
        readonly bool _asksAll;
        readonly bool _signalsOnly;

        StageColumn _blocks;
        KeyedChildren<long, PickRow> _blockRows;
        KindRows _kindRows;
        SlotListing _slotListing;
        ListField _reads;
        ICommand _clearKind;
        ICommand _toggleHidden;
        bool _listsSlots;
        Action _moveOn;
        long _anchor;
        bool _fits;
        int _worded;

        /// <summary>
        /// The blocks stage of a pick of <paramref name="picked" />, searched as <paramref name="screen" /> keeps its search, listing
        /// only the blocks <paramref name="admits" /> allows, or all of them for null. <paramref name="asksAll" /> asks how
        /// several blocks are read; <paramref name="signalsOnly" /> is a pick of what starts something for a signal slot; <paramref name="forReadings" /> is a pick of what an expression reads, several blocks always combined.
        /// </summary>
        public BlockStage(SprocketEditor editor, PickChoices picked, EditorPage screen, Func<GridEntity, bool> admits,
            bool forEvents, bool forTests, bool asksAll, bool signalsOnly, bool forReadings = false)
        {
            _forReadings = forReadings;
            _editor = editor;
            _picked = picked;
            _screen = screen;
            _forEvents = forEvents;
            _forTests = forTests;
            _asksAll = asksAll;
            _signalsOnly = signalsOnly;
            _listing = new BlockListing(editor, admits, forEvents, forTests);
        }

        /// <summary>The column the stage is shown in, or null until it is made.</summary>
        public StageColumn Column => _blocks;

        /// <summary>The slots listed to be picked by name, or null where blocks are listed.</summary>
        public SlotListing Slots => _slotListing;

        /// <summary>Lists the blocks on offer again, those <paramref name="fits" /> allows where it is given.</summary>
        public void Collect(Func<GridEntity, bool> fits)
        {
            _listing.Collect(_picked.Blocks, _screen.Search, _kinds.Kind, _kinds.Family, ShowsHidden, fits, _kinds.Present);
        }

        /// <summary>
        /// Makes the column, which lists the slots <paramref name="slotted" /> has where <paramref name="listsSlots" />, and
        /// <paramref name="moveOn" /> finishes as a second press in quick succession does.
        /// </summary>
        public StageColumn Make(float width, bool listsSlots, AutomationSystem slotted, bool fits, Action moveOn)
        {
            _listsSlots = listsSlots;
            _moveOn = moveOn;
            _fits = fits;

            _blocks = new StageColumn(width, true, null, _editor.Texts);
            _blocks.Explain(_editor.Codex);
            Say();

            // For the whole life of the column, so the line does not come and go as the list of kinds is narrowed.
            _blocks.HasKinds = !listsSlots && _kinds.Present.Count > 0;
            _blocks.Narrowed += OnBlockSearch;
            _blocks.KindsNarrowed += NarrowKinds;

            if (listsSlots)
                _slotListing = new SlotListing(_editor, slotted, _forEvents, _picked.Blocks, moveOn);
            else
                _blockRows = new KeyedChildren<long, PickRow>(_blocks.Rows, BlockRow);

            _kindRows = new KindRows(_kinds, _blocks.Kinds, KindPress);
            _blocks.KindsOpened += delegate
            {
                if (_editor.Input != null)
                    _editor.Input.Focus(_blocks.KindNarrow);
            };
            _clearKind = _editor.Act(CommandKind.AnyKind, () => SetKind(""));
            _toggleHidden = _editor.Act(CommandKind.HiddenBlocks, delegate
            {
                ShowsHidden = !ShowsHidden;
                _editor.Invalidate();
            });
            _blocks.ShowHidden = listsSlots ? null : _toggleHidden;

            return _blocks;
        }

        /// <summary>Says the column's title, its source and what it says while empty, in the language the editor speaks.</summary>
        void Say()
        {
            var texts = _editor.Texts;
            _worded = _editor.Rewordings;
            _blocks.Title = _listsSlots ? texts.PickSlots : texts.PickBlocks;

            _blocks.EmptyText = _listsSlots
                ? texts.PickNoSlots
                : _fits
                ? texts.BlueprintNoneFits
                : _forTests ? texts.PickNoBlocksChecked
                : _forEvents ? texts.PickNoBlocksHappen : texts.PickNoBlocksDone;
        }

        /// <summary>
        /// Shows what is on offer and what is picked; <paramref name="limit" /> names the one kind every block on offer is of, said
        /// where the list of kinds would say it offers any, or is null.
        /// </summary>
        public void Fill(string limit)
        {
            // Words read again in another language since the column was made are said again; once, not on every fill.
            if (_worded != _editor.Rewordings)
            {
                Say();
                _blocks.Say(_editor.Texts);
            }

            if (_listsSlots)
            {
                _slotListing.Show(_blocks, _screen.Search);
                FillOptions();
                _blocks.ShowEmpty();
                return;
            }

            var texts = _editor.Texts;
            var selection = _picked.Blocks;
            _blocks.Subtitle = selection.Count == 0
                ? texts.PickNonePicked
                : selection.Count == 1
                    ? texts.PickOnePicked
                    : Template.Fill(texts.PickSomePicked, "count", selection.Count.ToString(CultureInfo.InvariantCulture));

            // Offered whatever the length of the list.
            _blocks.NarrowText = _screen.Search;

            // Off the whole catalogue, so narrowing never takes the picture column away and shifts every row sideways.
            var icons = false;
            for (var i = 0; i < _listing.Candidates.Count && !icons; i++)
                icons = !string.IsNullOrEmpty(_listing.Candidates[i].Kind.Icon);

            FillFilters(limit);
            FillOptions();

            _blocks.Showing = _listing.Filtered.Count == _listing.Candidates.Count
                ? Template.Fill(texts.PickAllShown, "count", _listing.Candidates.Count.ToString(CultureInfo.InvariantCulture))
                : Template.Fill(Template.Fill(texts.PickSomeShown, "shown", _listing.Filtered.Count.ToString(CultureInfo.InvariantCulture)),
                    "count", _listing.Candidates.Count.ToString(CultureInfo.InvariantCulture));

            _blockRows.Show(_listing.FilteredIds);
            foreach (var entity in _listing.Filtered)
            {
                var row = _blockRows[entity.Id];

                row.Text = entity.Name;
                row.Secondary = Kind(entity);
                row.Grid = OtherGrid(entity);
                row.Icon = entity.Kind.Icon;
                row.HasPicture = icons;
                row.Many = true;
                row.IsChosen = BlockListing.Contains(selection, entity.Id);
                row.Show();
            }

            _blocks.ShowEmpty();
        }

        /// <summary>Whether the blocks hidden from toolbars are listed: the eye open.</summary>
        /// <remarks>The player's own setting in this world, the same in every picker.</remarks>
        bool ShowsHidden
        {
            get { return _editor.Viewer.ShowsHidden; }
            set
            {
                if (_editor.Viewer.ShowsHidden == value)
                    return;

                _editor.Viewer.ShowsHidden = value;
                if (_editor.Preferred != null)
                    _editor.Preferred();
            }
        }

        PickRow BlockRow(long id)
        {
            var row = new PickRow();
            row.Picked += (held, presses) => Picked(id, held, presses);
            row.Ticks = _editor.Act(CommandKind.PickEntity, () => Pick(id, HeldModifier.Also));
            return row;
        }

        /// <summary>What pressing the kinds row kept under <paramref name="kind" /> does: narrows the blocks to that kind or family, or to none.</summary>
        ICommand KindPress(string kind)
        {
            var family = KindFilter.FamilyOf(kind);
            return _editor.Act(kind.Length == 0 ? CommandKind.AnyKind : CommandKind.OneKind, delegate
            {
                _blocks.CloseKinds();
                if (family != Family.None)
                    SetFamily(family);
                else
                    SetKind(kind);
            });
        }

        /// <summary>Takes a block the way the press said, and moves on where a plain press was a second in quick succession.</summary>
        void Picked(long id, HeldModifier held, int presses)
        {
            var pick = _editor.Act(CommandKind.PickEntity, () => Pick(id, held));
            if (pick == null)
                return;

            pick.Execute();
            if (presses > 1 && held == HeldModifier.Alone)
                _moveOn();
        }

        /// <summary>The kind of <paramref name="entity" />, and the grid it is on where that is not the programmable block's own.</summary>
        string Kind(GridEntity entity)
        {
            var grid = OtherGrid(entity);
            if (grid.Length == 0)
                return entity.Kind.DisplayName;

            return Template.Fill(Template.Fill(_editor.Texts.PickOnGrid, "kind", entity.Kind.DisplayName), "grid", grid);
        }

        /// <summary>The name of the grid <paramref name="entity" /> is on where that is not the programmable block's own, or empty.</summary>
        string OtherGrid(GridEntity entity)
        {
            var home = _editor.World.Grid;
            var grid = entity.Block.CubeGrid;
            return home == null || grid == null || grid == home ? "" : grid.CustomName;
        }

        void FillFilters(string limit)
        {
            var texts = _editor.Texts;

            if (_kinds.Settle())
                _editor.Invalidate();

            // Narrowed to no kind, the blocks on offer are still those of the kind a slot limits them to, and it says so.
            var every = limit ?? texts.PickAnything;
            var current = _kindRows.Show(texts, every, _listing);
            var anything = _kindRows[""];
            anything.Tip = CodexTip.For(anything.Tip, limit == null ? null : Template.Fill(texts.Codex.PickLimitAbout, "kind", limit));

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

            var narrow = _editor.Act(CommandKind.NarrowKinds, delegate
            {
                _kinds.Search = said;
                _editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>
        /// Puts the choice of how several blocks are read under them, on a test and on a trigger watching for a state.
        /// </summary>
        void FillOptions()
        {
            var asks = _asksAll && (_forTests || _forEvents && !_signalsOnly
                && (_picked.Member.IsEmpty || Watched() || _picked.Form == ReachKind.Value || _picked.Totals));
            if (asks && _forReadings)
                asks = Combined();

            if (!asks)
            {
                _blocks.Options.Clear();
                return;
            }

            if (_reads == null)
            {
                var offered = _forReadings ? _picked.Checks ? Checkings : Combinings : SentenceText.Readings;
                var words = new List<string>();
                for (var i = 0; i < offered.Count; i++)
                    words.Add(SentenceText.Quantified(offered[i]));

                _reads = new ListField(words) { Tip = CodexTip.For(null, _editor.Texts.Codex.SequenceReadAbout) };
                _reads.Edited += delegate
                {
                    var reads = _editor.Act(CommandKind.ReadBlocks, () =>
                    {
                        var quantifier = Read(_reads.Text);
                        _picked.Checking = _forReadings && _picked.Checks && (quantifier == Quantifier.Any || quantifier == Quantifier.All);
                        Quantify(quantifier);
                    });
                    if (reads != null)
                        reads.Execute();
                };
            }

            if (_blocks.Options.Count == 0)
                _blocks.Options.Add(_reads);

            var said = SentenceText.Quantified(_picked.Reading);
            if (_reads.Text != said)
                _reads.Text = said;
        }

        /// <summary>
        /// Whether the blocks of a reading are combined: one block is read as it is, and several are always combined, as a sum
        /// where no way of combining them is chosen yet.
        /// </summary>
        bool Combined()
        {
            if (_picked.Blocks.Count < 2)
            {
                _picked.Checking = false;
                if (_picked.Totals)
                    Quantify(Quantifier.Any);

                return false;
            }

            if (!_picked.Totals && !_picked.Checking)
                Quantify(Quantifier.Sum);

            return true;
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

        /// <summary>Whether what is chosen to start something is a state Sprocket watches the blocks for, rather than a signal they send.</summary>
        bool Watched()
        {
            if (!_forEvents || _picked.Member.IsEmpty)
                return false;

            var events = Vocabulary.EventsOf(_editor.World, _picked.Blocks);
            foreach (var entityEvent in events)
            {
                if (entityEvent.Key == _picked.Member)
                    return !entityEvent.Answered && entityEvent.Value.Length > 0;
            }

            return false;
        }

        /// <summary>Reads the blocks as <paramref name="quantifier" /> says, letting go of what is chosen where a sum or average cannot be taken of it.</summary>
        void Quantify(Quantifier quantifier)
        {
            _picked.Reading = quantifier;

            string value, state;
            if (_picked.Totals && !_picked.Member.IsEmpty
                && (ComparisonQuestion.Split(_picked.Member.Id, out value, out state)
                    || !MemberOffers.Number(Vocabulary.ValueOf(_editor.World, _picked.Blocks, _picked.Member))))
            {
                _picked.Member = default(MemberKey);
            }

            _editor.Invalidate();
        }

        /// <summary>Takes a block, the way the press said it was taken.</summary>
        void Pick(long entityId, HeldModifier how)
        {
            if (how == HeldModifier.Through && _anchor != 0L)
            {
                Span(_anchor, entityId);
                _editor.Invalidate();
                return;
            }

            _anchor = entityId;

            if (how == HeldModifier.Also)
                Toggle(entityId);
            else
                Only(entityId);

            _editor.Invalidate();
        }

        void Toggle(long entityId)
        {
            if (BlockListing.Contains(_picked.Blocks, entityId))
                _picked.Blocks.Remove(entityId);
            else
                _picked.Blocks.Add(entityId);
        }

        /// <summary>Takes the block <paramref name="entityId" /> and nothing else, which is what pressing a row plainly means.</summary>
        void Only(long entityId)
        {
            _picked.Blocks.Clear();
            _picked.Blocks.Add(entityId);
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

            _picked.Blocks.Clear();
            for (var i = first; i <= last; i++)
                _picked.Blocks.Add(_listing.Filtered[i].Id);
        }

        /// <summary>Takes new search text, and ignores text that is not new.</summary>
        void OnBlockSearch(string value)
        {
            value = value ?? "";
            if (value == _screen.Search)
                return;

            var narrow = _editor.Act(CommandKind.NarrowEntities, delegate
            {
                _screen.Search = value;
                _editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }

        /// <summary>Narrows the blocks to one kind.</summary>
        void SetKind(string kindId)
        {
            _kinds.Set(kindId);
            _editor.Invalidate();
        }

        /// <summary>Narrows the blocks to every kind of <paramref name="family" />.</summary>
        void SetFamily(Family family)
        {
            _kinds.SetFamily(family);
            _editor.Invalidate();
        }
    }
}
