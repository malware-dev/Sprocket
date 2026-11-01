using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>
    /// The work of a logic system's installation panel tab: its description, and its slots and categories with the shown
    /// installation's blocks, put in order, grouped, filled, opened and tidied.
    /// </summary>
    public sealed class SlotPanelTab
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly SlotPanel _panel;
        readonly Func<CommandKind, Action, ICommand> _changes;
        readonly Func<string> _locked;
        readonly KeyedChildren<object, Element> _slotRows;
        readonly List<Part> _parts = new List<Part>();
        readonly List<object> _slotKeys = new List<object>();
        readonly List<string> _used = new List<string>();
        readonly List<string> _marks = new List<string>();
        readonly SlotFill _fill;
        readonly SlotArranger _arranger;
        readonly OptimizeQuestion _optimizing;

        Element _focused;
        string _naming;

        /// <summary>
        /// The installation panel tab of <paramref name="system" />, shown in <paramref name="panel" />, its changes made through
        /// commands <paramref name="changes" /> makes, and <paramref name="locked" /> saying why nothing can be changed where it cannot.
        /// </summary>
        public SlotPanelTab(SprocketEditor editor, AutomationSystem system, SlotPanel panel, Func<CommandKind, Action, ICommand> changes,
            Func<string> locked)
        {
            _editor = editor;
            _system = system;
            _panel = panel;
            _changes = changes;
            _locked = locked;
            _fill = new SlotFill(editor, system);
            _arranger = new SlotArranger(editor, system);
            _optimizing = new OptimizeQuestion(editor);

            var slots = panel.Slots;
            _slotRows = new KeyedChildren<object, Element>(slots.Children, SlotRow);
            slots.FnAllowedDropIndexes = index => SlotArranger.Landings(_slotKeys, index);
            slots.FnCanIndent = (from, to) => SlotArranger.Nests(_slotKeys, from, to);
            slots.FnIsIndented = index => SlotArranger.StandsIn(_slotKeys, index);
            slots.Base.IndentWidth = Sizes.Indent;
            slots.Reordered += moved =>
            {
                var move = _editor.Act(CommandKind.MoveSlot, () => _arranger.Move(_slotKeys, moved.From, moved.To, moved.IsIndented));
                if (move != null)
                    move.Execute();
            };
        }

        bool Editable => _locked().Length == 0;

        /// <summary>Lays the tab out again from the blueprint's description and slots.</summary>
        /// <remarks>Walks every slot and installation to say what each still has to do, so it runs only when the page is built.</remarks>
        public void Arrange(Texts texts)
        {
            ArrangeDescription(texts);
            ArrangeSlots(texts);
            _panel.Empty = !_panel.Describing.IsVisible && _parts.Count == 0 && !Editable ? texts.SystemNoPanel : "";
        }

        /// <summary>Writes the description box into the blueprint once the keyboard has left it.</summary>
        /// <remarks>Called every tick.</remarks>
        public void Refocused()
        {
            var now = _editor.Input?.Focused;
            if (ReferenceEquals(now, _focused))
                return;

            var left = _focused;
            _focused = now;
            Written(left);
        }

        /// <summary>Writes the focused box into the blueprint and lets the keyboard go.</summary>
        public void Settle()
        {
            Written(_focused);
            _focused = null;
            if (_editor.Input != null)
                _editor.Input.Focus(null);
        }

        /// <summary>Writes the description being typed into the blueprint, on the way off the page.</summary>
        public void Leave()
        {
            Written(_focused);
            _focused = null;
        }

        /// <summary>Writes what was typed in <paramref name="box" /> into the blueprint, where it is the description's box.</summary>
        void Written(Element box)
        {
            var composer = _panel.Composer;
            if (box == null || !Editable || box != composer.Box)
                return;

            _editor.Do(_system, new AnnotateSystem(_system.Annotation, composer.Text));
        }

        /// <summary>Shows a blueprint's description, in its box while it may be changed, leaving alone a box being typed in.</summary>
        void ArrangeDescription(Texts texts)
        {
            var blueprint = _system.IsBlueprint;
            var editable = Editable;
            var said = _system.Annotation;

            _panel.Describing.IsVisible = blueprint && (editable || said.Length > 0);
            if (!_panel.Describing.IsVisible)
                return;

            var composer = _panel.Composer;
            _panel.DescriptionHeading.IsVisible = editable;
            _panel.DescriptionHeading.Text = texts.BlueprintDescription.ToUpperInvariant();
            composer.IsVisible = editable;
            composer.Show(texts);
            composer.Guides = _editor.Guides;
            composer.Hint = texts.BlueprintDescriptionHint;
            if (!composer.Box.IsFocused)
                composer.Text = said;

            _panel.Description.IsVisible = !editable;
            _panel.Description.Markdown = said;
        }

        /// <summary>Lists a blueprint's slots, each with the blocks the shown installation fills it with.</summary>
        void ArrangeSlots(Texts texts)
        {
            var shown = _system.IsBlueprint ? _editor.StandIn(_system) : null;
            var editable = Editable;
            var locked = _locked();

            _parts.Clear();
            _slotKeys.Clear();
            _system.Acted(_used);
            if (shown != null)
                PartList.Listed(_system, _parts, _slotKeys, true);

            var add = _panel.AddCategory;
            var optimize = _panel.Optimize;
            _panel.Slotting.IsVisible = _parts.Count > 0;
            _panel.SlotsHeading.Markdown = CodexPages.Link(texts.BlueprintParts.ToUpperInvariant(), CodexPages.Slot);
            add.IsVisible = editable;
            add.Text = StatusRow.Plus + texts.CategoryAdd;
            // A category just added opens its name for typing once its row stands.
            add.Command = _changes(CommandKind.AddCategory, () => _naming = _arranger.Add(_editor.Texts));
            add.Tip = CodexTip.For(add.Tip, texts.Codex.TermCategory);
            optimize.Text = texts.SlotOptimize;
            optimize.Command = _changes(CommandKind.OptimizeSlots, () => _optimizing.Ask(_system));
            optimize.Tip = editable ? CodexTip.For(optimize.Tip, texts.Codex.SlotOptimizeAbout) : KeptTip.For(optimize.Tip, locked);
            _slotRows.Show(_slotKeys);
            if (shown == null)
                return;

            for (var i = 0; i < _slotKeys.Count; i++)
            {
                var heading = _slotKeys[i] as CategoryHeading;
                if (heading != null)
                    ArrangeCategory(texts, (CategoryRow)_slotRows[heading], heading, SlotArranger.Carries(_slotKeys, i));
            }

            foreach (var part in _parts)
            {
                var row = (PartRow)_slotRows[part];
                row.Arranged = true;
                row.Indented = part.Category.Length > 0;
                row.Grips(editable, editable ? texts.Codex.SlotDrag : locked);

                row.Name = part.Id;
                row.Short = part.IsSignal ? part.Short : "";
                row.Mark = MarkText.OfSlot(part);
                row.Described = part.Annotation;
                row.Opens = _changes(CommandKind.OpenSlot, () => _editor.Push(new SlotPage(_editor, _system, part)));
                row.OpensAbout = editable ? texts.Codex.SlotOpenAbout : locked;
                row.FillsLocked = locked;
                row.Fills = _changes(CommandKind.FillPart, () => _fill.Ask(part, shown));

                // A slot nothing uses asks nothing of any installation, and is offered its bin.
                var unused = !_used.Contains(part.Id);
                row.Marks = unused ? null : Marked(texts, part, shown);
                row.Take(_editor.Runner.Bindings, shown.FilledWith(part.Id), part.IsSignal ? shown.Positions(part.Id) : null);
                row.Problem = unused ? texts.SlotUnused : null;
                row.Level = NoticeLevel.Warning;
                row.Removable = unused;
                row.Removes = unused ? _changes(CommandKind.RemoveSlot, () => _editor.Do(_system, ChangeSlots.Removing(_system, shown, part))) : null;
                row.RemovesAbout = editable ? texts.Codex.SlotRemoveAbout : locked;

                if (row.Blocks.Count > 0)
                    row.Show(_editor.World);
                else
                    row.Say(SentenceText.Empty, !part.Optional && !unused);
            }
        }

        /// <summary>A row for <paramref name="key" /> of the installation panel: a slot's, or a category's.</summary>
        Element SlotRow(object key)
        {
            if (key is Part)
                return new PartRow(_editor.Texts);

            return new CategoryRow();
        }

        /// <summary>Shows the category <paramref name="heading" /> heads over the <paramref name="carries" /> slots under it, renamed, removed and carried while the system may be changed.</summary>
        void ArrangeCategory(Texts texts, CategoryRow row, CategoryHeading heading, int carries)
        {
            var editable = Editable;
            var locked = _locked();
            var name = heading.Name;

            row.Name = name;
            row.DraggedWithCount = carries;
            row.Arranged = true;
            row.Grips(editable, editable ? texts.Codex.CategoryDrag : locked);
            row.Renames = _changes(CommandKind.RenameCategory, () => _editor.Rename(row.Plate, new Naming
            {
                Name = name,
                NameRequired = true,
                Accepts = SlotArranger.Accepts,
                Renames = wanted => _arranger.Rename(heading, wanted)
            }));
            row.RenamesAbout = editable ? texts.Codex.CategoryRenameAbout : locked;
            if (name == _naming)
            {
                _naming = null;
                row.Renames.Execute();
            }

            row.Removes = _changes(CommandKind.RemoveCategory, () => _arranger.Remove(heading));
            row.RemovesAbout = editable ? texts.Codex.CategoryRemove : locked;
        }

        /// <summary>What the installations other than <paramref name="shown" /> still have to do about <paramref name="part" />, or null where nothing.</summary>
        string Marked(Texts texts, Part part, SystemInstance shown)
        {
            _marks.Clear();
            var instances = _system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i] == shown)
                    continue;

                var mark = SlotMarks.Of(_editor.Runner.Bindings, instances[i], part);
                if (mark == SlotMark.None)
                    continue;

                var said = mark == SlotMark.Changed ? texts.SlotMarkChanged : mark == SlotMark.Lost ? texts.SlotMarkLost : texts.SlotMarkNew;
                _marks.Add(Template.Fill(said, "installation", SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, _system, instances[i])));
            }

            return _marks.Count == 0 ? null : string.Join("; ", _marks.ToArray());
        }
    }
}
