using Sprocket.Host.SpaceEngineers.Features.Installations.Edits;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System.Collections.Generic;
using Lumel;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Installations
{
    /// <summary>A blueprint's installation panel: one installation's slots filled with its blocks.</summary>
    public sealed partial class BlueprintPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly List<Part> _parts = new List<Part>();
        readonly List<object> _rows = new List<object>();
        readonly List<string> _used = new List<string>();
        readonly SlotFill _fill;

        SystemInstance _made;
        string _madeName;
        bool _madeOn;
        NoticeLevel _standing;
        int _shape = -1;
        KeyedChildren<object, Element> _partRows;

        /// <summary>The installation panel of the blueprint <paramref name="system" />, in the editor <paramref name="editor" />, filling an installation's slots.</summary>
        public BlueprintPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
            _fill = new SlotFill(editor, system);
        }

        /// <summary>The installation whose blocks fill the slots.</summary>
        public override string Title => Blockset == null
            ? _system.Name
            : SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, Blockset);

        /// <inheritdoc />
        public override bool Edits => true;

        /// <inheritdoc />
        public override string CustomName => Title;

        /// <summary>The installation can be renamed here; the panel is named for its blueprint, which is renamed on its logic system screen.</summary>
        public override bool CanRename => Blockset != null;

        /// <summary>The installation's own switch.</summary>
        public override bool CanSwitch => Blockset != null;

        /// <summary>The blockset's own setting, which it keeps while the blueprint is off.</summary>
        public override bool SwitchIsOn => Blockset != null && Blockset.Enabled;

        /// <summary>How the installation stands, as its row on the home screen does: something wrong, a warning, or neither.</summary>
        public override NoticeLevel Standing => _standing;

        /// <summary>The blockset's switch stops that one and nothing else made from the blueprint; while the blueprint is off, it says to switch that on first.</summary>
        public override string SwitchAbout => _system.Enabled ? StatusRow.Switching(Editor.Texts, false, SwitchIsOn) : Editor.Texts.Codex.RowSwitchBlueprintFirst;

        /// <summary>The blockset's switch waits for the blueprint's while that is off.</summary>
        public override bool SwitchLocked => !_system.Enabled;

        /// <summary>The blockset just made is as it was made: its name, its switch, and not one slot filled.</summary>
        public override bool Untouched =>
            _made != null && _made.Name == _madeName && _made.Enabled == _madeOn && _made.IsEmpty;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.Codex.BlueprintNameAbout;

        /// <summary>An installation has to be called something, since it is told from the others by its name.</summary>
        public override bool NameRequired => true;

        /// <summary>The way back of the logic system the installation is one of: the last thing done to it, on any of its screens.</summary>
        public override bool HasHistory => true;

        /// <summary>A step is walked back under the rule it was made under, which each step says for itself.</summary>
        public override bool CanUndo => Editor.Holder == null && Editor.CanUndo(_system, Blockset);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_system, Blockset);

        /// <inheritdoc />
        public override bool CanRedo => Editor.Holder == null && Editor.CanRedo(_system, Blockset);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_system, Blockset);

        /// <summary>The blueprint's parts, one row each, with the chosen logic system's blocks for each.</summary>
        public Element.Collection Parts => _partList.Children;

        /// <summary>The logic system whose blocks fill the slots, or null before there is one.</summary>
        SystemInstance Blockset => Editor.StandIn(_system);

        /// <inheritdoc />
        public override bool IsAbout(AutomationSystem system)
        {
            return system == _system;
        }

        /// <summary>Switches the blockset being filled on or off, and nothing else made from the blueprint.</summary>
        public override void Switch(bool on)
        {
            var chosen = Blockset;
            if (chosen != null)
                Editor.SwitchInstance(_system, chosen, on);
        }

        /// <inheritdoc />
        public override bool AcceptsName(string name)
        {
            return NameRules.Clean(name).Length > 0;
        }

        /// <inheritdoc />
        public override void Rename(string name)
        {
            var wanted = NameRules.Clean(name);
            if (!AcceptsName(wanted))
                return;

            if (Blockset != null)
                Editor.Do(_system, new RenameInstallation(Blockset, Blockset.Name, wanted));
        }

        /// <summary>
        /// Reads as a path from the logic system to the installation: the logic system first, as a part of its own that opens
        /// its screen, then the installation's name, the two joined by the heading's arrow.
        /// </summary>
        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            parts[0].Mark = Blockset == null ? Symbols.LogicSystem : Symbols.Installation;
            parts[0].MarkAbout = Blockset == null ? Editor.Texts.Codex.TermLogicSystem : Editor.Texts.Codex.TermInstallation;

            // Pushed, so Back returns to the blocks being filled.
            var from = new HeadingPart(_system.Name);
            from.Mark = Symbols.LogicSystem;
            from.MarkAbout = Editor.Texts.Codex.TermLogicSystem;
            from.About = Template.Fill(Editor.Texts.Codex.SystemsEditBlueprint, "name", _system.Name);
            from.Opens = Editor.Act(CommandKind.OpenSystem, () => Editor.Push(new LogicSystemPage(Editor, _system)));
            parts.Insert(0, from);
        }

        /// <inheritdoc />
        public override void RefreshHeading(List<HeadingPart> parts)
        {
            if (parts.Count < 2)
            {
                base.RefreshHeading(parts);
                return;
            }

            parts[0].Text = _system.Name;
            parts[1].Text = Title;
            parts[1].Name = CustomName;
        }

        /// <inheritdoc />
        public override void Undo()
        {
            Editor.Undo(_system, Blockset);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            Editor.Redo(_system, Blockset);
        }

        /// <inheritdoc />
        public override void Unmake()
        {
            if (_made != null)
                Editor.TakeOut(_system, _made);
        }

        /// <summary>Notes the blockset just made, as it was made.</summary>
        protected override void Remember()
        {
            _made = Blockset;
            if (_made == null)
                return;

            _madeName = _made.Name;
            _madeOn = _made.Enabled;
        }

        /// <summary>Brings the view up to date.</summary>
        public override void Build()
        {
            _shape = -1;
            Show();
        }

        /// <summary>Builds the screen again where how many installations there are or which is chosen has changed.</summary>
        /// <remarks>Every tick, and walks nothing: the slots change only through the screens under it, which rebuild it on the way back.</remarks>
        public override void Refresh()
        {
            if (Shape(Editor.StandIn(_system)) != _shape)
            {
                Editor.Invalidate();
                return;
            }

            if (Revised())
                Troubled(Blockset);
        }

        /// <summary>Keeps the rows by slot, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _partRows = new KeyedChildren<object, Element>(_partList.Children, Row);
            Editor.Codex.Explain(_partsHeading);
            Show();
        }

        void Show()
        {
            if (_partRows == null)
                return;

            var texts = Editor.Texts;
            var chosen = Blockset;
            _shape = Shape(chosen);

            _partsHeading.Markdown = CodexPages.Term(texts.BlueprintParts.ToUpperInvariant(), CodexPages.Slot);
            Describe();

            Collect();
            _partRows.Show(_rows);
            foreach (var row in _rows)
            {
                var heading = row as CategoryHeading;
                if (heading != null)
                    ((CategoryRow)_partRows[row]).Name = heading.Name;
            }

            var unfilled = 0;
            foreach (var part in _parts)
            {
                ((PartRow)_partRows[part]).Indented = part.Category.Length > 0;
                if (!Part((PartRow)_partRows[part], part, chosen) && !part.Optional)
                    unfilled++;
            }


            Needs(chosen, unfilled);
            Troubled(chosen);

            var runs = chosen != null && Live(chosen);
            _locked.IsVisible = runs;
            _locked.Text = runs ? Template.Fill(texts.SystemLocked, "name", Title) : "";
            _rule.IsVisible = _description.IsVisible || _troubled.IsVisible;
        }

        /// <summary>Says what is wrong with the installation being filled, and offers the sequence at fault where that is where it is mended.</summary>
        void Troubled(SystemInstance chosen)
        {
            Sequence sequence = null;
            var said = "";
            var mended = false;
            var empty = false;
            var restored = false;

            if (chosen != null && !chosen.Enabled && chosen.Halt != null)
            {
                restored = SequenceText.Lost(chosen.Halt) && SystemSummary.Problems(Editor.Runner, _system, chosen) == 0;
                said = SequenceText.Stopped(Editor.Texts, chosen.Halt, _system, restored);
                sequence = restored ? null : _system.FindSequence(chosen.Halt.Sequence);
                empty = chosen.Halt.Problem == SequenceProblem.EmptySlot;
                mended = true;
            }
            else if (chosen != null)
            {
                SequenceFault fault;
                sequence = SystemSummary.Failing(Editor.Runner, _system, chosen, out fault)
                    ?? SystemSummary.Warned(Editor.Runner, _system, chosen, out fault);
                said = sequence == null ? "" : SequenceText.Failing(Editor.Texts, fault, sequence);
                mended = fault.IsWarning;
                empty = fault.Problem == SequenceProblem.EmptySlot;
            }

            // Switched off with nothing wrong, as a new installation is made to be filled in, it says so; the heading's switch turns it on.
            var filled = chosen != null && !chosen.Enabled && chosen.Halt == null && _system.Enabled && said.Length == 0;
            if (filled)
            {
                said = Editor.Texts.BlueprintFilled;
                mended = true;
            }

            _troubled.IsVisible = said.Length > 0;
            _trouble.Text = said;
            _trouble.Style = mended ? Styles.WarningLine : Styles.ProblemLine;
            // As the installation's row on the home screen stands: wrong while anything counts against it, warned while it says something else.
            _standing = chosen != null && SystemSummary.Problems(Editor.Runner, _system, chosen) > 0 ? NoticeLevel.Problem
                : said.Length > 0 ? NoticeLevel.Warning : NoticeLevel.Info;

            // An empty slot is filled on this page, so it opens no sequence.
            var opens = sequence != null && !empty && !restored && !filled;
            _openTrouble.IsVisible = opens;
            _openTrouble.Text = opens ? Template.Fill(Editor.Texts.BlueprintOpenSequence, "name", sequence.Name) : "";
            _openTrouble.Command = opens
                ? Editor.Act(CommandKind.OpenSequence, delegate
                {
                    Editor.Choose(_system, chosen);
                    Editor.Push(new SequencePage(Editor, sequence));
                })
                : null;
        }

        /// <summary>Shows what the blueprint's author wrote about it.</summary>
        void Describe()
        {
            var said = _system.Annotation;
            _description.IsVisible = said.Length > 0;
            _description.Markdown = said;
        }

        /// <summary>
        /// Beside the slots' heading, how many slots changed kind and are to be filled again, or otherwise how many of
        /// those that need a block still have none; nothing once every one has.
        /// </summary>
        void Needs(SystemInstance chosen, int unfilled)
        {
            var texts = Editor.Texts;
            var again = chosen == null ? 0 : SlotMarks.Changed(Editor.Runner.Bindings, _system, chosen, _used);
            var count = again > 0 ? again : unfilled;

            _needs.IsVisible = chosen != null && count > 0;
            _needs.Text = again > 0
                ? again == 1 ? texts.SlotFillAgainOne : Template.Fill(texts.SlotFillAgainMany, "count", Count(again))
                : unfilled == 1 ? texts.BlueprintStillOne : Template.Fill(texts.BlueprintStillMany, "count", Count(unfilled));
        }

        static string Count(int count)
        {
            return count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>A slot, with the blocks <paramref name="chosen" /> fills it with and a press on them that chooses others; false where no block fills it.</summary>
        bool Part(PartRow row, Part part, SystemInstance chosen)
        {
            row.Name = part.Id;
            row.Short = part.IsSignal ? part.Short : "";
            row.Mark = MarkText.OfSlot(part);
            row.Described = part.Annotation;

            var gone = row.Take(Editor.Runner.Bindings, chosen?.FilledWith(part.Id),
                chosen == null || !part.IsSignal ? null : chosen.Positions(part.Id));

            row.Fills = chosen == null || Live(chosen) ? null : Editor.Act(CommandKind.FillPart, () => _fill.Ask(part, chosen));

            row.Problem = gone != null ? Template.Fill(Editor.Texts.BlueprintGone, "name", Called(gone)) : null;
            row.Level = NoticeLevel.Problem;

            var changed = chosen != null && SlotMarks.Of(Editor.Runner.Bindings, chosen, part) == SlotMark.Changed;

            if (row.Blocks.Count > 0)
                row.Show(Editor.World);
            else if (changed)
                row.Say(Editor.Texts.SlotChanged, true);
            else
                row.Say(SentenceText.Empty, !part.Optional);

            return row.Blocks.Count > 0;
        }

        /// <summary>What the block a reference names was called in the game, where the reference remembers, or its name in the program.</summary>
        string Called(string reference)
        {
            return Editor.Program.Called(reference, Editor.World.Values);
        }

        /// <summary>The slots the system's sentences use, and those slots in the order they are listed, under their categories.</summary>
        /// <remarks>A slot nothing uses is left to the logic system's own screen, and a category left with none goes with it.</remarks>
        void Collect()
        {
            _system.Acted(_used);
            PartList.Listed(_system, _parts, _rows, false);

            _parts.RemoveAll(part => !_used.Contains(part.Id));
            _rows.RemoveAll(row => row is Part && !_used.Contains(((Part)row).Id));
            for (var i = _rows.Count - 1; i >= 0; i--)
            {
                var heading = _rows[i] as CategoryHeading;
                var next = i + 1 < _rows.Count ? _rows[i + 1] as Part : null;
                if (heading != null && (next == null || next.Category != heading.Name))
                    _rows.RemoveAt(i);
            }
        }

        /// <summary>A row for <paramref name="key" />: a slot's, or a category's name, which here only says it.</summary>
        Element Row(object key)
        {
            if (key is Part)
                return new PartRow(Editor.Texts) { Plain = true };

            return new CategoryRow { Arranged = false };
        }

        /// <summary>Whether <paramref name="chosen" /> may run now, which keeps its blocks as they are: switched on, in a logic system switched on.</summary>
        bool Live(SystemInstance chosen)
        {
            return _system.Enabled && chosen.Enabled;
        }

        /// <summary>What the page is built from that can change under it, as one number: how many of it, which is chosen, and whether that one may run.</summary>
        int Shape(SystemInstance chosen)
        {
            var shape = _system.Instances.Count * 31 + (chosen?.Number ?? 0);
            return shape * 2 + (chosen != null && Live(chosen) ? 1 : 0);
        }
    }
}
