using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// The logic systems on this construct, and the blueprints some of them are made from. The front door.
    /// </summary>
    /// <remarks>
    /// Not a list of sequences. Nobody built a procedure - they built an airlock, and that is what they
    /// came back to look at.
    /// <para>
    /// A logic system of its own is one row. Making a blueprint of it is the act that brings the
    /// blueprint into view: it is named then, and it heads the logic systems made from it, so the idea
    /// arrives when somebody reaches for it rather than before they have made anything.
    /// </para>
    /// </remarks>
    sealed class SystemsPage : EditorPage
    {
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();

        /// <summary>Every logic system shown, with the row showing it, in the order they are listed.</summary>
        readonly List<Shown> _shown = new List<Shown>();

        /// <summary>The rows being built for one list, before they are published in one go.</summary>
        readonly List<StatusEntry> _gathered = new List<StatusEntry>();

        /// <summary>What the page was built from, so a change of shape rebuilds it.</summary>
        int _shape;

        /// <summary>One logic system's row, and what it is the row of.</summary>
        sealed class Shown
        {
            public StatusEntry Entry;
            public AutomationSystem System;
            public SystemInstance Instance;
        }

        public SystemsPage(SprocketEditor editor) : base(editor)
        {
        }

        /// <summary>
        /// What the construct is called, with what this screen is about led in front of it.
        /// </summary>
        /// <remarks>
        /// The construct's own name, so the one screen somebody arrives on tells them where they have
        /// arrived. A fixed heading reads the same on every Sprocket in the game, and "your ship" makes
        /// two claims besides: not every construct is a ship, and on a server it is the faction's.
        /// <para>
        /// The lead is here because this is <b>not</b> an overview of the machine. It is the automation on
        /// it, and a heading that was only a name would be promising the rest.
        /// </para>
        /// </remarks>
        public override string Title => Editor.World.Name;

        public override string CustomName => Editor.World.Name;

        /// <summary>
        /// Reaching outside the automation to rename the machine itself, which is deliberate.
        /// </summary>
        /// <remarks>
        /// It is the one thing on this screen that is not automation, and it is here because the name is
        /// already here and being read. Somebody looking at a construct called "Large Grid 8321" and
        /// wanting it called something else should not have to leave, find the terminal, and come back.
        /// </remarks>
        public override bool CanRename => Editor.World.CanRename;

        public override string RenamePrompt => "What is this called?";

        public override bool NameRequired => true;

        public override void Rename(string name)
        {
            var wanted = (name ?? "").Trim();
            if (wanted.Length == 0)
                return;

            Editor.World.Rename(wanted);
        }

        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);
            parts[0].Lead = "Automation for";
        }

        public override void Build(List<EditorComponent> components)
        {
            Collect();
            _shape = Shape();

            var start = new StartComponent();
            start.Add("New logic system", Editor.Register(Cmd.NewSystem, Make));
            start.Add("Load blueprint", Editor.Register(Cmd.LoadSystem, delegate { Editor.OfferLoading(); }));
            components.Add(start);

            _shown.Clear();

            // Logic systems of their own first, with nothing over them: a heading of theirs would be the
            // one thing on the screen asking to be understood before anything is made. The blueprints
            // follow, so a row of its own is never read as belonging to the blueprint above it.
            var plain = new StatusListComponent();
            plain.EmptyText = "Nothing automated here yet.";
            _gathered.Clear();
            for (var i = 0; i < _systems.Count; i++)
            {
                if (!_systems[i].IsBlueprint)
                    _gathered.Add(Plain(_systems[i]));
            }

            plain.Entries.Reset(_gathered);
            if (_gathered.Count > 0 || _systems.Count == 0)
                components.Add(plain);

            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].IsBlueprint)
                    Blueprint(components, _systems[i]);
            }
        }

        public override void Refresh()
        {
            Collect();
            if (Shape() != _shape)
            {
                Editor.Invalidate();
                return;
            }

            var recount = SecondPassed();
            for (var i = 0; i < _shown.Count; i++)
            {
                var shown = _shown[i];
                var entry = shown.Entry;

                if (recount)
                {
                    entry.Problems = SystemSummary.Problems(
                        Editor.World, Editor.Runner.Bindings, Editor.Runner, shown.System, shown.Instance);

                    entry.Remarks = SystemSummary.Remarks(
                        Editor.World, Editor.Runner.Bindings, Editor.Runner, shown.System, shown.Instance);
                }

                entry.SwitchIsOn = shown.Instance.Enabled;

                // This logic system, not the blueprint: with two airlocks, the port one cycling is not the
                // front one cycling. The ground says it as well as the word, the same way a row leading to
                // something broken is washed rather than only marked.
                var going = SystemSummary.IsRunning(shown.System, Editor.Runner, shown.Instance);
                entry.Status = SystemSummary.RunningText(going);
                entry.Running = going;
            }
        }

        /// <summary>A logic system of its own: opened by its row, switched by its switch, and made a blueprint of.</summary>
        StatusEntry Plain(AutomationSystem system)
        {
            var entry = new StatusEntry(SystemSummary.Describe(Editor.Runner.Bindings, system),
                Editor.Register(Cmd.OpenSystem, system.Id, delegate { Editor.Push(new SystemPage(Editor, system)); }));

            Switched(entry, system, system.First);

            // Said in words on the row, where it cannot be missed. Making a blueprint is how one logic
            // system becomes several, and a way to do that nobody finds is the problem this row solves.
            entry.ButtonText = "Make blueprint";
            entry.ButtonCommand = Editor.Register(Cmd.MakeBlueprint, system.Id, delegate { Made(system); });
            return entry;
        }

        /// <summary>A blueprint's heading, the logic systems made from it, and the way to add another.</summary>
        void Blueprint(List<EditorComponent> components, AutomationSystem system)
        {
            var heading = new HeadingComponent("Made from");
            heading.Name = system.Name;
            heading.After = "blueprint";
            heading.Follows = true;
            components.Add(heading);

            var list = new StatusListComponent();
            _gathered.Clear();

            var instances = system.Instances;
            for (var n = 0; n < instances.Count; n++)
            {
                var instance = instances[n];
                var entry = new StatusEntry(SystemSummary.DescribeInstance(Editor.Runner.Bindings, system, instance),
                    Editor.Register(Cmd.OpenSystem, system.Id, instance.Number, delegate
                    {
                        Editor.Choose(system, instance);
                        Editor.Push(new BlueprintPage(Editor, system));
                    }));
                entry.Depth = 1;
                Switched(entry, system, instance);
                _gathered.Add(entry);
            }

            var another = new StatusEntry("add another " + system.Name,
                Editor.Register(Cmd.AddInstance, system.Id, delegate { Another(system); }));
            another.IsAction = true;
            another.Depth = 1;
            _gathered.Add(another);

            list.Entries.Reset(_gathered);
            components.Add(list);
        }

        /// <summary>Gives a logic system's row its switch, which stops that one and nothing else.</summary>
        void Switched(StatusEntry entry, AutomationSystem system, SystemInstance instance)
        {
            if (instance == null)
                return;

            entry.SwitchCommand = Editor.RegisterToggle(Cmd.EnableInstance, system.Id, instance.Number,
                delegate(bool on) { instance.Enabled = on; });
            entry.SwitchIsOn = instance.Enabled;
            entry.SwitchGovernsAll = true;

            _shown.Add(new Shown { Entry = entry, System = system, Instance = instance });
        }

        /// <summary>Makes a logic system, numbered, and opens it with its name in hand to be given.</summary>
        void Make()
        {
            var system = Editor.Program.AddSystem();
            system.Namespace = Editor.Author;

            var page = new SystemPage(Editor, system);
            page.NameOnArrival = true;
            Editor.Push(page);
        }

        /// <summary>Makes a blueprint of a logic system: the blueprint keeps the system's name, and so does the first logic system made from it.</summary>
        void Made(AutomationSystem system)
        {
            if (system.First == null)
                return;

            system.First.Name = system.Name;
            Editor.Choose(system, system.First);
            Editor.Push(new BlueprintPage(Editor, system));
        }

        /// <summary>Adds another logic system made from a blueprint, numbered, and opens the blueprint on it to say which blocks it uses.</summary>
        void Another(AutomationSystem system)
        {
            if (Editor.Another(system) != null)
                Editor.Push(new BlueprintPage(Editor, system));
        }

        /// <summary>What the page is built from, as one number: which systems, how many of each, and which are blueprints.</summary>
        int Shape()
        {
            var shape = _systems.Count;
            for (var i = 0; i < _systems.Count; i++)
            {
                AutomationSystem system = _systems[i];
                shape = shape * 31 + system.Id;
                shape = shape * 31 + system.Instances.Count * 2 + (system.IsBlueprint ? 1 : 0);
            }

            return shape;
        }

        void Collect()
        {
            _systems.Clear();
            var systems = Editor.Program.Systems;
            for (var i = 0; i < systems.Count; i++)
                _systems.Add(systems[i]);
        }
    }
}
