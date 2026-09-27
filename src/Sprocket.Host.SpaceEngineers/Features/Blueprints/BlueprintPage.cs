using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>A blueprint: the logic systems made from it, and which blocks the chosen one fills each part with.</summary>
    /// <remarks>
    /// Where a blueprint is set up, and the one way into what it does. Its sentences are edited through one of its logic
    /// systems, because a sentence names blocks and only a logic system has any, so the way in says whose blocks it is using,
    /// from the one place that shows the choice. The two lists stand side by side: pointing at another logic system on the
    /// left, with its blocks changing on the right while the parts stay put, is the difference between a blueprint and what
    /// is made from it, seen rather than explained. The elements it is drawn with are declared in <c>BlueprintPage.View.cs</c>.
    /// </remarks>
    sealed partial class BlueprintPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly List<Part> _parts = new List<Part>();
        readonly List<string> _used = new List<string>();
        readonly List<SystemInstance> _instances = new List<SystemInstance>();

        int _shape = -1;
        Keyed<SystemInstance, StatusRow> _memberRows;
        Keyed<Part, PartRow> _partRows;
        Prompt _prompt;

        /// <summary>The page of the blueprint <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public BlueprintPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
        }

        /// <inheritdoc />
        public override string Title => _system.Name;

        /// <inheritdoc />
        public override string CustomName => _system.Name;

        /// <inheritdoc />
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.BlueprintNameAbout;

        /// <summary>A blueprint has to be called something, since it is told from the others by its name.</summary>
        public override bool NameRequired => true;

        /// <summary>The logic systems made from the blueprint, one row each, in the order they were made.</summary>
        public Ui.Element.Collection Members => _made.Children;

        /// <summary>The row that makes another logic system from the blueprint.</summary>
        public StatusRow Another => _another;

        /// <summary>The chosen logic system at the head of its blocks: its switch, and the way into what the blueprint does.</summary>
        public StatusRow Chosen => _chosen;

        /// <summary>The blueprint's parts, one row each, with the chosen logic system's blocks for each.</summary>
        public Ui.Element.Collection Parts => _partList.Children;

        /// <summary>The question a part's new name is typed into, where one has been asked.</summary>
        public Prompt Prompt => _prompt;

        /// <inheritdoc />
        public override bool AcceptsName(string name)
        {
            var other = Editor.Program.FindSystem(_system.Namespace, Naming.Clean(name));
            return other == null || other == _system;
        }

        /// <inheritdoc />
        public override void Rename(string name)
        {
            var wanted = Naming.Clean(name);
            if (wanted.Length == 0 || !AcceptsName(wanted))
                return;

            _system.Name = wanted;
        }

        /// <inheritdoc />
        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            parts[0].After = Editor.Texts.SystemBlueprint;
        }

        /// <summary>Brings the view up to date.</summary>
        public override void Build()
        {
            _shape = -1;
            Show();
        }

        /// <summary>Brings what the chosen logic system is doing up to date, and builds the screen again where how many there are or which is chosen has changed.</summary>
        /// <remarks>
        /// Every tick, and it walks nothing: the parts change only through this page, which rebuilds itself when they do, and a
        /// sentence changes only on the screens under it, which rebuild it on the way back.
        /// </remarks>
        public override void Refresh()
        {
            if (Shape(Editor.StandIn(_system)) != _shape)
            {
                Editor.Invalidate();
                return;
            }

            Live();
        }

        /// <summary>Gives the row that makes another its press, keeps the rows by logic system and by part, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _another.Opens = Editor.Act(Cmd.AddInstance, delegate { Editor.Another(_system); });
            _another.IsAction = true;
            _memberRows = new Keyed<SystemInstance, StatusRow>(_made.Children, Member);
            _partRows = new Keyed<Part, PartRow>(_partList.Children, PartRow);
            Show();
        }

        void Show()
        {
            if (_memberRows == null)
                return;

            var texts = Editor.Texts;
            var chosen = Editor.StandIn(_system);
            _shape = Shape(chosen);

            _madeHeading.Text = texts.BlueprintMade.ToUpperInvariant();
            _partsHeading.Text = texts.BlueprintParts.ToUpperInvariant();

            _instances.Clear();
            _instances.AddRange(_system.Instances);
            _memberRows.Show(_instances);
            for (var i = 0; i < _instances.Count; i++)
            {
                var row = _memberRows[_instances[i]];
                row.Summary = SystemSummary.DescribeInstance(texts, Editor.Runner.Bindings, _system, _instances[i]);
                row.Chosen = _instances[i] == chosen;
                row.Show(texts);
            }

            _another.Summary = Template.Fill(texts.BlueprintAnother, "name", _system.Name);
            _another.Show(texts);

            _chosen.IsVisible = chosen != null;
            if (chosen != null)
                Head(chosen);

            Collect();
            _partRows.Show(_parts);
            for (var i = 0; i < _parts.Count; i++)
                Part(_partRows[_parts[i]], _parts[i], chosen);

            Live();
        }

        /// <summary>What the chosen logic system is doing and whether it is switched on, read every tick because neither costs anything.</summary>
        void Live()
        {
            var chosen = Editor.StandIn(_system);
            if (_memberRows == null || chosen == null)
                return;

            var going = SystemSummary.IsRunning(_system, Editor.Runner, chosen);
            _chosen.SwitchIsOn = chosen.Enabled;
            _chosen.Status = SystemSummary.RunningText(Editor.Texts, going);
            _chosen.Running = going;
            _chosen.Show(Editor.Texts);
        }

        /// <summary>One of the logic systems made from the blueprint, chosen by pressing it.</summary>
        StatusRow Member(SystemInstance instance)
        {
            return new StatusRow { Opens = Editor.Act(Cmd.ChooseInstance, delegate { Editor.Choose(_system, instance); }) };
        }

        /// <summary>The chosen logic system at the head of its blocks: its switch, and the way into what the blueprint does, through these blocks.</summary>
        void Head(SystemInstance chosen)
        {
            _chosen.Summary = SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, chosen);
            _chosen.SwitchGovernsAll = true;
            _chosen.Flips = Editor.Act(Cmd.EnableInstance, delegate
            {
                chosen.Enabled = !chosen.Enabled;
                Editor.Invalidate();
            });

            _chosen.ButtonText = Editor.Texts.BlueprintEditUsing;
            _chosen.Offers = Editor.Act(Cmd.EditBlueprint, delegate { Editor.Push(new LogicSystemPage(Editor, _system)); });
        }

        PartRow PartRow(Part part)
        {
            var row = new PartRow();
            row.Renames = Editor.Act(Cmd.RenamePart, delegate { Relabelling(part, row); });
            return row;
        }

        /// <summary>A part, with the blocks <paramref name="chosen" /> fills it with, and a press on them that chooses others.</summary>
        void Part(PartRow row, Part part, SystemInstance chosen)
        {
            row.Name = part.Label;
            row.About = part.About;
            row.Blocks.Clear();

            var filled = chosen == null ? null : chosen.FilledWith(part.Id);
            if (filled != null)
            {
                for (var i = 0; i < filled.Count; i++)
                {
                    var block = Editor.Runner.Bindings.Entity(filled[i]);
                    if (block != null)
                        row.Blocks.Add(block.Id);
                }
            }

            row.Fills = chosen == null ? null : Editor.Act(Cmd.FillPart, delegate { Fill(part, chosen); });
            row.Show(Editor.World);
        }

        /// <summary>Asks the part's new name, in a box hanging under its pencil.</summary>
        void Relabelling(Part part, PartRow row)
        {
            if (_prompt == null)
                _prompt = new Prompt(Editor.Texts.PromptContinue);

            _prompt.Ask(Editor.Texts.BlueprintRenamePart, part.Label, row.Pencil,
                delegate(string name) { return Naming.Clean(name).Length > 0; },
                delegate(string name)
                {
                    var rename = Editor.Act(Cmd.RenamePart, delegate { Relabel(part, name); });
                    if (rename != null)
                        rename.Execute();
                },
                Editor.Windows, Editor.Input);
        }

        /// <summary>Asks which of the construct's blocks fills <paramref name="part" /> for <paramref name="chosen" />.</summary>
        /// <remarks>
        /// Only blocks of the types the part expects, where it says: a door's part is offered doors. The block it holds now is
        /// marked, so choosing is also seeing what is there.
        /// </remarks>
        void Fill(Part part, SystemInstance chosen)
        {
            var page = new ChoicePage(Editor, part.Label);
            page.Caption = SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, chosen);
            page.EmptyText = Editor.Texts.BlueprintNoneFits;

            var filled = chosen.FilledWith(part.Id);
            var entities = Editor.World.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                var block = entities[i];
                if (!Fits(part, block))
                    continue;

                var current = filled != null && filled.Count == 1 && Editor.Runner.Bindings.Entity(filled[0]) == block;
                page.Add(block.Name, current, delegate { Filled(part, chosen, block); });
            }

            Editor.Push(page);
        }

        /// <summary>Whether <paramref name="block" /> is of a type <paramref name="part" /> may be filled with.</summary>
        static bool Fits(Part part, GridEntity block)
        {
            if (part.Expects.Count == 0)
                return true;

            var sort = BlockKinds.Sort(block.Kind.Id);
            for (var i = 0; i < part.Expects.Count; i++)
            {
                var expects = part.Expects[i];
                if (string.Equals(sort, expects, StringComparison.Ordinal)
                    || sort.StartsWith(expects + "/", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>Fills <paramref name="part" /> for <paramref name="chosen" /> with <paramref name="block" />, and goes back to the blueprint.</summary>
        /// <remarks>Rewired as any edit is, because what a trigger's button is may have been this part.</remarks>
        void Filled(Part part, SystemInstance chosen, GridEntity block)
        {
            chosen.Fill(part.Id, new[] { Editor.Program.Declare(Editor.World, block).Name });
            Editor.Program.Edited();
            Editor.Program.Rewire();
            Editor.Pop();
            Editor.Invalidate();
        }

        void Relabel(Part part, string name)
        {
            name = Naming.Clean(name);
            if (name.Length == 0)
                return;

            part.Relabel(name);
            Editor.Program.Edited();
            Editor.Invalidate();
        }

        /// <summary>The parts the system's sentences use, in the order the system declares them.</summary>
        void Collect()
        {
            _parts.Clear();
            _system.Acted(_used);

            var parts = _system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (_used.Contains(parts[i].Id))
                    _parts.Add(parts[i]);
            }
        }

        /// <summary>What the page is built from that can change under it, as one number: how many of it, and which is chosen.</summary>
        int Shape(SystemInstance chosen)
        {
            return _system.Instances.Count * 31 + (chosen == null ? 0 : chosen.Number);
        }
    }
}
