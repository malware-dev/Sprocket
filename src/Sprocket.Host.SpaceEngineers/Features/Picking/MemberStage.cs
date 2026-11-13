using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// The second stage of a pick: what is done to the blocks, checked of them or happens to them, listed in the view that
    /// Sprocket vouches for or the terminal's, with the facet the members act on and the slots of a toolbar a person presses.
    /// </summary>
    public class MemberStage
    {
        // Keys among the rows that no row's own key can be.
        const string SlotsKey = "slots";

        const string FacetsKey = "facets";

        readonly SprocketEditor _editor;
        readonly PickChoices _picked;
        readonly bool _forEvents;
        readonly bool _forTests;
        readonly MemberOffers _offers;
        readonly SlotOccupancy _occupancy;
        readonly List<int> _matched = new List<int>();
        readonly List<int> _rows = new List<int>();
        readonly List<string> _whatKeys = new List<string>();
        readonly List<string> _whatShown = new List<string>();
        readonly Dictionary<string, int> _whatAt = new Dictionary<string, int>();

        StageColumn _what;
        KeyedChildren<string, Element> _whatRows;
        SlotStrip _slots;
        FacetChoice _facets;
        ICommand _organized;
        ICommand _terminal;
        Action _moveOn;
        string _whatSearch = "";
        int _worded;
        string _dropped = "";
        ReachKind _droppedForm;

        // Whether the column has been shown in the view that lists what the page was opened on, which it is once.
        bool _viewFound;

        /// <summary>
        /// The member stage of a pick of <paramref name="picked" />: what happens to the blocks where <paramref name="forEvents" />,
        /// what can be checked of them where <paramref name="forTests" />, only what starts something for a signal slot where
        /// <paramref name="signalsOnly" />, and otherwise what can be done to them.
        /// </summary>
        public MemberStage(SprocketEditor editor, PickChoices picked, bool forEvents, bool forTests, bool signalsOnly)
        {
            _editor = editor;
            _picked = picked;
            _forEvents = forEvents;
            _forTests = forTests;
            _offers = new MemberOffers(editor, forEvents, forTests, signalsOnly);
            _occupancy = new SlotOccupancy(editor);
        }

        /// <summary>The column the stage is shown in, or null where it is not made or has been let go.</summary>
        public StageColumn Column => _what;

        /// <summary>Whether <paramref name="member" />, reached as <paramref name="form" /> says, can be made into a step, as the offers last listed it.</summary>
        public Readiness ReadinessOf(string member, ReachKind form) => _offers.ReadinessOf(member, form);

        /// <summary>The name what is chosen is listed under, or empty where nothing is.</summary>
        public string ChosenName
        {
            get
            {
                var at = _offers.At(_picked.Member, _picked.Form);
                return at < 0 ? "" : _offers.Names[at];
            }
        }

        /// <summary>What the codex says of what is chosen in the organized list, or empty where it says nothing.</summary>
        public string ChosenAbout
        {
            get
            {
                var at = _offers.At(_picked.Member, _picked.Form);
                var block = _picked.Blocks.Count == 0 ? null : _editor.World.FindEntity(_picked.Blocks[0]);
                if (at < 0 || block == null || _editor.ShowsTerminal)
                    return "";

                return _editor.Codex.Member(block.Kind, _offers.Names[at], _offers.IsStatusWord(at));
            }
        }

        /// <summary>Makes the column, <paramref name="width" /> wide, whose second press in quick succession on a row does <paramref name="moveOn" />.</summary>
        public StageColumn Make(float width, Action moveOn)
        {
            _moveOn = moveOn;
            _what = new StageColumn(width, true, Views(_editor.Texts), _editor.Texts);
            Say();
            _what.Narrowed += OnWhatSearch;
            _whatRows = new KeyedChildren<string, Element>(_what.Rows, WhatRow);

            _organized = _editor.Act(CommandKind.ShowOrganized, () => Show(false));
            _terminal = _editor.Act(CommandKind.ShowTerminal, () => Show(true));

            return _what;
        }

        /// <summary>
        /// The words of the two views of what is done to a block, checked of one or happens to one: what Sprocket vouches for,
        /// and the rest, in <paramref name="texts" />.
        /// </summary>
        /// <remarks>The angle marks point the way each link goes; the words alone are translated.</remarks>
        static string[] Views(Texts texts) => new[] { $"‹ {texts.PickToOrganized}", $"{texts.PickToTerminal} ›" };

        /// <summary>Says the column's title, its views and what it says while empty, in the language the editor speaks.</summary>
        void Say()
        {
            var texts = _editor.Texts;
            _worded = _editor.Rewordings;
            _what.NameViews(Views(texts), new[] { texts.Codex.PickToOrganizedAbout, texts.Codex.PickToTerminalAbout });
            _what.Title = _forTests ? texts.PickWhatToCheck : _forEvents ? texts.PickWhatHappens : texts.PickWhatToDo;
            _what.EmptyText = _forTests
                ? texts.PickNothingChecked
                : _forEvents ? texts.PickNothingHappens : texts.PickNothingDone;
        }

        /// <summary>Lets the column go, where there is nothing for it to be about.</summary>
        public void Drop()
        {
            _what = null;
        }

        /// <summary>
        /// Lists what is on offer, only the members of <paramref name="slotType" /> where it is given, and only the member
        /// <paramref name="only" /> where it is given.
        /// </summary>
        public void Fill(HostType slotType, string only)
        {
            // Words read again in another language since the column was made are said again; once, not on every fill.
            if (_worded != _editor.Rewordings)
            {
                Say();
                _what.Say(_editor.Texts);
            }

            var selection = _picked.Blocks;
            _offers.Fill(selection, _picked.Totals, only);
            FindView(only);

            var texts = _editor.Texts;
            var note = "";
            if (_forTests || _forEvents)
            {
                note = _editor.ShowsTerminal ? texts.PickTerminalNote : "";
                var totals = _picked.Reading == Quantifier.Sum ? texts.PickSumNote : _picked.Reading == Quantifier.Average ? texts.PickAverageNote : "";
                if (totals.Length > 0)
                    note = note.Length == 0 ? totals : $"{note} {totals}";
            }
            else if (!_forEvents && !_forTests)
            {
                note = _editor.ShowsTerminal ? texts.PickTerminalNote : PlanText.Note(texts, Vocabulary.Planned(_editor.World, selection));
                if (Vocabulary.Narrowed(_editor.World, selection, _editor.ShowsTerminal))
                    note = note.Length == 0
                        ? texts.PickShared
                        : Template.Fill(Template.Fill(texts.PickNoteAndShared, "shared", texts.PickShared), "note", note);
            }

            // A choice the blocks can no longer honor is dropped, so Done never shows over a sentence that could not run, and
            // kept aside until something else is chosen: blocks that honor it again bring it back with its values.
            if (_picked.Member.Length > 0 && _offers.At(_picked.Member, _picked.Form) < 0)
            {
                _dropped = _picked.Member;
                _droppedForm = _picked.Form;
                _picked.Member = "";
                _picked.Form = ReachKind.Native;
            }
            else if (_picked.Member.Length == 0 && _dropped.Length > 0 && _offers.At(_dropped, _droppedForm) >= 0)
            {
                _picked.Member = _dropped;
                _picked.Form = _droppedForm;
                _dropped = "";
            }

            var chosen = _picked.Member;

            _what.Note = note;
            _what.NarrowText = _whatSearch;
            _what.ShowViews(_editor.ShowsTerminal ? 1 : 0, new[] { _organized, _terminal });

            _matched.Clear();
            for (var i = 0; i < _offers.Names.Count; i++)
            {
                // A slot of a type the host provides offers that type's members alone, on the facet the slot holds.
                if (slotType != null && _offers.Types[i] != slotType || slotType == null && _offers.InSlotsOnly[i])
                    continue;

                // What is chosen stays listed whatever the search, as a chosen block does, so a search never hides the
                // answer this picker opened with.
                if (EntityQuery.MatchesText(_offers.Names[i], _whatSearch) || _offers.Ids[i] == _picked.Member)
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
            var shared = type == null || slotType != null ? null : Facets.Shared(_editor.World, selection, type);
            if (shared != null)
                Keep(shared);

            var facets = shared != null && shared.Count > 1;
            if (facets)
                _whatShown.Insert(first, FacetsKey);

            // The slots of a toolbar a person presses open under its line once it is chosen.
            var pressed = SlotOccupancy.Pressing(_editor.World, selection, _picked.Member);
            var under = pressed == null ? -1 : _whatShown.IndexOf($"{ReachKind.Native} {pressed.Id}");
            if (under >= 0)
                _whatShown.Insert(under + 1, SlotsKey);

            _whatRows.Show(_whatShown);
            if (under >= 0)
                ShowSlots(pressed);
            else
                _slots = null;

            if (facets)
                _facets.Show(shared, _picked.Facet, Facets.Showing(_editor.World, selection, type, _picked.Facet));
            else
                _facets = null;

            var lastIndented = -1;
            for (var i = 0; facets && i < _rows.Count; i++)
            {
                if (_rows[i] >= 0 && _offers.Types[_rows[i]] == type)
                    lastIndented = i;
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var at = _rows[i];
                var row = (PickRow)_whatRows[_whatKeys[i]];
                row.Many = false;
                row.HasPicture = false;
                row.Secondary = "";
                row.Indented = facets && at >= 0 && _offers.Types[at] == type;
                row.EndsSpine = i == lastIndented;

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
                row.Status = ReadinessText.Tag(_editor.Texts, ready);
                row.IsChosen = _offers.Ids[at] == chosen && form == _picked.Form;
                row.Mark = _offers.Marks.Count > at ? _offers.Marks[at] : "";
                row.About = MarkText.About(texts, row.Mark);
                row.Show();
            }

            _what.ShowEmpty();
        }

        /// <summary>
        /// Chooses the toolbar a person presses that has a slot read as <paramref name="words" />, "button 2 is pressed", and
        /// then that slot, as pressing the toolbar's line and then the slot under it does; false where no slot reads so.
        /// </summary>
        public bool ChooseSlot(string words)
        {
            if (!_forEvents)
                return false;

            foreach (var pressed in Vocabulary.EventsOf(_editor.World, _picked.Blocks))
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

        Element WhatRow(string key)
        {
            if (key == SlotsKey)
            {
                _slots = new SlotStrip(_editor.Texts);
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

        /// <summary>Chooses what the row under <paramref name="key" /> offers, and moves on where it was pressed twice in quick succession.</summary>
        void Chosen(string key, int presses)
        {
            int at;
            if (!_whatAt.TryGetValue(key, out at) || at < 0)
                return;

            var id = _offers.Ids[at];
            var form = _offers.Forms.Count > at ? _offers.Forms[at] : ReachKind.Native;
            var choose = _editor.Act(CommandKind.PickChoice, () => Choose(id, form));
            if (choose == null)
                return;

            choose.Execute();
            if (presses > 1)
                _moveOn();
        }

        /// <summary>
        /// Shows, the first time the column is filled, the view that lists what the page was opened on, so whatever made a
        /// choice edits it; the view showing stays where it lists it, or where neither does.
        /// </summary>
        void FindView(string only)
        {
            if (_viewFound)
                return;

            _viewFound = true;
            if (_picked.Member.Length == 0 || _offers.At(_picked.Member, _picked.Form) >= 0)
                return;

            _editor.ShowsTerminal = !_editor.ShowsTerminal;
            _offers.Fill(_picked.Blocks, _picked.Totals, only);
            if (_offers.At(_picked.Member, _picked.Form) >= 0)
                return;

            _editor.ShowsTerminal = !_editor.ShowsTerminal;
            _offers.Fill(_picked.Blocks, _picked.Totals, only);
        }

        void Show(bool terminal)
        {
            _editor.ShowsTerminal = terminal;
            _editor.Invalidate();
        }

        void ShowSlots(EntityEvent pressed)
        {
            _occupancy.Occupy(_picked.Blocks, pressed);
            if (_picked.Slot == null)
                _picked.Slot = _occupancy.Free(_picked.Blocks, pressed);

            _slots.Show(pressed.Pages, pressed.Slots, _occupancy.Held, _picked.Slot.Page, _picked.Slot.Slot);
        }

        void PickSlot(int page, int slot)
        {
            var whole = ToolbarSlot.Of(_picked.Member);
            if (whole == null)
                return;

            var pick = _editor.Act(CommandKind.PickChoice, delegate
            {
                _picked.Slot = new ToolbarSlot(whole.Toolbar, page, slot);
                _editor.Invalidate();
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

        /// <summary>Keeps the facet chosen where the blocks still share it, and otherwise chooses the first they share, or none.</summary>
        void Keep(List<string> shared)
        {
            if (shared == null || shared.Count == 0)
                _picked.Facet = "";
            else if (!shared.Contains(_picked.Facet))
                _picked.Facet = shared[0];
        }

        void PickFacet(string name)
        {
            var pick = _editor.Act(CommandKind.PickChoice, delegate
            {
                _picked.Facet = name;
                _editor.Invalidate();
            });
            if (pick != null)
                pick.Execute();
        }

        void Choose(string id, ReachKind form)
        {
            var pressed = form == ReachKind.Native ? SlotOccupancy.Pressing(_editor.World, _picked.Blocks, id) : null;
            if (pressed == null)
                _picked.Slot = null;
            else if (id != _picked.Member || _picked.Slot == null)
                _picked.Slot = _occupancy.Free(_picked.Blocks, pressed);

            _picked.Member = id;
            _picked.Form = form;
            _dropped = "";
            _editor.Invalidate();
        }

        void OnWhatSearch(string value)
        {
            value = value ?? "";
            if (value == _whatSearch)
                return;

            var narrow = _editor.Act(CommandKind.NarrowChoices, delegate
            {
                _whatSearch = value;
                _editor.Invalidate();
            });

            if (narrow != null)
                narrow.Execute();
        }
    }
}
