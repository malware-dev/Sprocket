using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar
{
    /// <summary>
    /// The slots a logic system already has, listed in a picker's first column to be picked by name: each with its mark and how
    /// many blocks fill it, the one chosen with what it holds on a row under it.
    /// </summary>
    public sealed class SlotListing
    {
        // Before the name of the slot a row says the holding of; a slot's name is one line, so it never holds this.
        const string HeldKey = "\n";

        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly bool _forEvents;
        readonly List<long> _selection;
        readonly Action _moveOn;
        readonly List<string> _picked = new List<string>();
        readonly List<string> _listed = new List<string>();
        readonly List<Mention> _heldBlocks = new List<Mention>();
        readonly List<Mention> _heldAnswers = new List<Mention>();
        readonly HashSet<string> _heldSaid = new HashSet<string>();
        KeyedChildren<string, Element> _rows;
        Part _whole;

        /// <summary>
        /// The slots of <paramref name="system" /> listed in <paramref name="editor" />, signal slots among them for what starts
        /// something where <paramref name="forEvents" />; picking one puts the blocks that stand for what is picked in
        /// <paramref name="selection" />, and a second plain press in quick succession carries out <paramref name="moveOn" />.
        /// </summary>
        public SlotListing(SprocketEditor editor, AutomationSystem system, bool forEvents, List<long> selection, Action moveOn)
        {
            _editor = editor;
            _system = system;
            _forEvents = forEvents;
            _selection = selection;
            _moveOn = moveOn;
        }

        /// <summary>The slots picked, by name, in the order they were picked.</summary>
        public IList<string> Picked => _picked;

        /// <summary>Whether a signal slot is picked, which is all a trigger needs to know of what starts it.</summary>
        public bool PicksSignal => _whole != null && _whole.IsSignal;

        /// <summary>The type the host provides that the slot picked holds, whose members alone are offered for it; null for any other.</summary>
        public HostType Type { get; private set; }

        /// <summary>The facet the slot picked holds of its type, which what it does acts on; empty for any other.</summary>
        public string Facet { get; private set; } = "";

        /// <summary>Lists the slots in <paramref name="column" />, as <paramref name="search" /> narrows them, the column's counts and headings with them.</summary>
        public void Show(StageColumn column, string search)
        {
            if (_rows == null)
                _rows = new KeyedChildren<string, Element>(column.Rows, Row);

            var texts = _editor.Texts;
            var picked = _picked.Count;
            column.Subtitle = picked == 0
                ? texts.PickNonePicked
                : picked == 1
                    ? texts.PickOnePicked
                    : Template.Fill(texts.PickSomePicked, "count", picked.ToString(CultureInfo.InvariantCulture));

            column.NarrowText = search;
            column.Showing = "";
            column.ShowsHidden = false;

            _listed.Clear();
            foreach (var part in _system.Parts)
            {
                if (!(Lists(part) && EntityQuery.MatchesText(part.Id, search) || _picked.Contains(part.Id)))
                    continue;

                _listed.Add(part.Id);
                if (_picked.Contains(part.Id))
                    _listed.Add(HeldKey + part.Id);
            }

            _rows.Show(_listed);
            var one = new string[1];
            foreach (var slot in _listed)
            {
                if (slot.StartsWith(HeldKey, StringComparison.Ordinal))
                {
                    ShowHeld((SlotHeldRow)_rows[slot], slot.Substring(HeldKey.Length));
                    continue;
                }

                one[0] = slot;
                var row = (PickRow)_rows[slot];
                row.Text = slot;
                row.Secondary = _picked.Contains(slot) ? "" : Fills(_editor.Blocks(_system, one).Count);
                row.Icon = null;
                row.Mark = MarkText.OfSlot(_system.FindPart(slot));
                row.HasPicture = false;
                row.Many = true;
                row.IsChosen = _picked.Contains(slot);
                row.Show();
            }
        }

        /// <summary>How many blocks fill a slot in the installation shown, <paramref name="count" />, in words.</summary>
        string Fills(int count)
        {
            var texts = _editor.Texts;
            return count == 1 ? texts.PickSlotFillsOne : Template.Fill(texts.PickSlotFills, "count", count.ToString(CultureInfo.InvariantCulture));
        }

        Element Row(string slot)
        {
            if (slot.StartsWith(HeldKey, StringComparison.Ordinal))
                return new SlotHeldRow();

            var row = new PickRow();
            row.Picked += (held, presses) => Pressed(slot, held, presses);
            row.Ticks = _editor.Act(CommandKind.PickEntity, () => Take(slot, HeldModifier.Also));
            return row;
        }

        /// <summary>Takes a slot the way the press said, and moves on where a plain press was a second in quick succession.</summary>
        void Pressed(string slot, HeldModifier held, int presses)
        {
            var pick = _editor.Act(CommandKind.PickEntity, () => Take(slot, held));
            if (pick == null)
                return;

            pick.Execute();
            if (presses > 1 && held == HeldModifier.Alone)
                _moveOn();
        }

        /// <summary>
        /// Takes the slot <paramref name="slot" />, alone or beside those taken already, and the blocks that stand for what is
        /// picked with it; a signal slot or a slot of a type the host provides is taken alone.
        /// </summary>
        void Take(string slot, HeldModifier how)
        {
            var part = _system.FindPart(slot);
            var whole = part != null && (part.IsSignal || part.Holds.Length != 0);
            if (how == HeldModifier.Alone || whole || _whole != null)
                _picked.Clear();

            if (!_picked.Remove(slot))
                _picked.Add(slot);

            _whole = whole && _picked.Count > 0 ? part : null;
            Type = _whole == null ? null : HostTypes.Named(_whole.Holds);
            Facet = Type == null ? "" : _editor.Picks.Facet(_system, _picked);

            _selection.Clear();
            foreach (var id in _editor.SlotBlocks.Standing(_system, _picked))
            {
                if (!BlockListing.Contains(_selection, id))
                    _selection.Add(id);
            }

            _editor.Invalidate();
        }

        /// <summary>
        /// Shows on <paramref name="row" /> what the installation shown fills <paramref name="slot" /> with: its blocks as every list
        /// of a slot's blocks says them, and beside them the signal each answers with or the screen each holds.
        /// </summary>
        void ShowHeld(SlotHeldRow row, string slot)
        {
            var part = _system.FindPart(slot);
            var shown = _editor.StandIn(_system);
            var filled = shown?.FilledWith(slot);
            var positions = shown?.Positions(slot);
            var facets = shown?.Facets(slot);
            var type = part == null ? null : HostTypes.Named(part.Holds);
            var bindings = _editor.Runner.Bindings;
            var world = _editor.World;

            var ids = new List<long>();
            _heldAnswers.Clear();
            _heldSaid.Clear();
            for (var i = 0; filled != null && i < filled.Count; i++)
            {
                GridEntity block = bindings.Entity(filled[i]);
                if (block == null)
                    continue;

                ids.Add(block.Id);
                var signal = positions != null && i < positions.Count ? block.Kind.Signal(positions[i]) : null;
                var facet = type != null && facets != null && i < facets.Count ? type.Find(block.Kind, facets[i]) : null;
                var said = signal != null ? SentenceText.InTrigger(world, new EventSlot(signal, null)) : facet?.Name;
                if (said == null || !_heldSaid.Add(said))
                    continue;

                if (_heldAnswers.Count > 0)
                    _heldAnswers.Add(new Mention(", ", null));

                if (signal != null)
                    SentenceText.InTrigger(world, new EventSlot(signal, null), _heldAnswers);
                else
                    _heldAnswers.Add(new Mention(said, null));
            }

            _heldBlocks.Clear();
            HeldBlocks.List(_editor.Texts, world, ids, part == null || !part.IsSignal, _heldBlocks);
            row.Show(_heldBlocks, _heldAnswers);
        }

        /// <summary>Whether the column lists <paramref name="part" />: a block slot always, a signal slot for what starts something, a slot of a type the host provides for what is done or checked.</summary>
        bool Lists(Part part)
        {
            if (part.IsSetting)
                return false;

            if (part.IsSignal)
                return _forEvents;

            return part.Holds.Length == 0 || !_forEvents;
        }
    }
}
