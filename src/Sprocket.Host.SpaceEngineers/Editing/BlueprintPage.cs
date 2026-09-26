using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Blueprints;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// A blueprint: the logic systems made from it, and which blocks the chosen one fills each part with.
    /// </summary>
    /// <remarks>
    /// Where a blueprint is set up, and the one way into what it does. Its sentences are edited through
    /// one of its logic systems, because a sentence names blocks and only a logic system has any - so
    /// the way in says whose blocks it is using, from the one place that shows the choice.
    /// </remarks>
    sealed class BlueprintPage : EditorPage
    {
        readonly AutomationSystem _system;

        readonly List<Part> _parts = new List<Part>();

        readonly List<string> _used = new List<string>();

        BlueprintComponent _blueprint;

        int _shape;

        public BlueprintPage(SprocketEditor editor, AutomationSystem system) : base(editor)
        {
            _system = system;
        }

        public override string Title => _system.Name;

        public override string CustomName => _system.Name;

        public override bool CanRename => true;

        public override string RenamePrompt => "What is the blueprint called?";

        public override string NameAbout => "This blueprint's name.\nPress to rename it.";

        public override bool NameRequired => true;

        public override bool AcceptsName(string name)
        {
            var other = Editor.Program.FindSystem(_system.Namespace, Naming.Clean(name));
            return other == null || other == _system;
        }

        public override void Rename(string name)
        {
            var wanted = Naming.Clean(name);
            if (wanted.Length == 0 || !AcceptsName(wanted))
                return;

            _system.Name = wanted;
        }

        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            parts[0].After = "blueprint";
        }

        public override void Build(List<EditorComponent> components)
        {
            SystemInstance chosen = Editor.StandIn(_system);
            _shape = Shape(chosen);

            _blueprint = new BlueprintComponent();
            _blueprint.MadeTitle = "Made from it";
            _blueprint.PartsTitle = "Parts";
            _blueprint.RenamePrompt = "What is this part called?";

            var instances = _system.Instances;
            for (var i = 0; i < instances.Count; i++)
                _blueprint.Members.Add(Member(instances[i], instances[i] == chosen));

            var another = new StatusEntry("add another " + _system.Name,
                Editor.Register(Cmd.AddInstance, _system.Id, delegate { Editor.Another(_system); }));
            another.IsAction = true;
            _blueprint.Members.Add(another);

            if (chosen != null)
                _blueprint.Chosen = Head(chosen);

            Collect();
            for (var i = 0; i < _parts.Count; i++)
                _blueprint.Parts.Add(Entry(i, _parts[i], chosen));

            components.Add(_blueprint);
        }

        public override void Refresh()
        {
            SystemInstance chosen = Editor.StandIn(_system);
            if (Shape(chosen) != _shape)
            {
                Editor.Invalidate();
                return;
            }

            if (_blueprint == null || _blueprint.Chosen == null || chosen == null)
                return;

            var head = _blueprint.Chosen;
            head.SwitchIsOn = chosen.Enabled;

            var going = SystemSummary.IsRunning(_system, Editor.Runner, chosen);
            head.Status = SystemSummary.RunningText(Editor.Texts, going);
            head.Running = going;
        }

        /// <summary>One of the logic systems made from it, chosen by pressing it.</summary>
        StatusEntry Member(SystemInstance instance, bool chosen)
        {
            var entry = new StatusEntry(SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, instance),
                Editor.Register(Cmd.ChooseInstance, _system.Id, instance.Number,
                    delegate { Editor.Choose(_system, instance); }));
            entry.Chosen = chosen;
            return entry;
        }

        /// <summary>
        /// The chosen logic system at the head of its blocks: its switch, and the way into what the
        /// blueprint does, through these blocks.
        /// </summary>
        StatusEntry Head(SystemInstance chosen)
        {
            var head = new StatusEntry(SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, chosen), 0);
            head.SwitchCommand = Editor.RegisterToggle(Cmd.EnableInstance, _system.Id, chosen.Number,
                delegate(bool on) { chosen.Enabled = on; });
            head.SwitchIsOn = chosen.Enabled;
            head.SwitchGovernsAll = true;

            head.ButtonText = "Edit using these blocks";
            head.ButtonCommand = Editor.Register(Cmd.EditBlueprint, _system.Id,
                delegate { Editor.Push(new LogicSystemPage(Editor, _system)); });
            return head;
        }

        /// <summary>A part, with the blocks <paramref name="chosen" /> fills it with.</summary>
        PartEntry Entry(int index, Part part, SystemInstance chosen)
        {
            var entry = new PartEntry(part.Label);
            entry.About = part.About;

            var filled = chosen == null ? null : chosen.FilledWith(part.Id);
            if (filled != null)
            {
                for (var i = 0; i < filled.Count; i++)
                {
                    GridEntity block = Editor.Runner.Bindings.Entity(filled[i]);
                    if (block != null)
                        entry.Blocks.Add(block.Id);
                }
            }

            if (chosen != null)
                entry.Command = Editor.Register(Cmd.FillPart, index, delegate { Fill(part, chosen); });

            entry.RenameCommand = Editor.RegisterText(Cmd.RenamePart, index,
                delegate(string name) { Relabel(part, name); },
                delegate(string name) { return Naming.Clean(name).Length > 0; });
            return entry;
        }

        /// <summary>Asks which of the construct's blocks fills <paramref name="part" /> for <paramref name="chosen" />.</summary>
        /// <remarks>
        /// Only blocks of the types the part expects, where it says: a door's part is offered doors. The
        /// block it holds now is marked, so choosing is also seeing what is there.
        /// </remarks>
        void Fill(Part part, SystemInstance chosen)
        {
            var page = new ChoicePage(Editor, part.Label);
            page.Caption = SystemSummary.DescribeInstance(Editor.Texts, Editor.Runner.Bindings, _system, chosen);
            page.EmptyText = "Nothing on this construct can fill it.";

            var filled = chosen.FilledWith(part.Id);
            var entities = Editor.World.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                GridEntity block = entities[i];
                if (!Fits(part, block))
                    continue;

                var current = filled != null && filled.Count == 1
                    && Editor.Runner.Bindings.Entity(filled[0]) == block;
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

        /// <summary>
        /// Fills <paramref name="part" /> for <paramref name="chosen" /> with <paramref name="block" />, and
        /// goes back to the blueprint.
        /// </summary>
        /// <remarks>
        /// Rewired as any edit is, because what a trigger's button is may have been this part.
        /// </remarks>
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
        /// <remarks>
        /// Asked every tick, so it walks nothing. The parts change only through this page, which rebuilds
        /// itself when they do, and a sentence changes only on the screens under it, which rebuild it on
        /// the way back.
        /// </remarks>
        int Shape(SystemInstance chosen)
        {
            return _system.Instances.Count * 31 + (chosen == null ? 0 : chosen.Number);
        }
    }
}
