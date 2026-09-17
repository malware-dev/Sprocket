using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Screen;

namespace Reactor.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// The systems on this ship, and the ones of each that are actually fitted. The front
    /// door.
    /// </summary>
    /// <remarks>
    /// Not a list of sequences. Nobody built a procedure - they built an airlock, and that is
    /// what they came back to look at.
    ///
    /// A system always shows the ones of it underneath, even when there is only one. That is
    /// what makes the naming work: <c>Airlock 1</c> reads as provisional and gets renamed
    /// without a dialog asking for it, and a system and the one of it never share a row and
    /// end up saying the same thing twice.
    /// </remarks>
    sealed class SystemsPage : EditorPage
    {
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();
        readonly List<AutomationSystem> _rowOwners = new List<AutomationSystem>();
        readonly List<SystemInstance> _rowInstances = new List<SystemInstance>();

        /// <summary>
        /// How many of each system were fitted when its row was last told so.
        /// </summary>
        /// <remarks>
        /// Saying it takes joining three strings and formatting a number, and this runs every tick for
        /// every row on the screen. The count changes when somebody builds or grinds something.
        /// </remarks>
        readonly List<int> _rowFitted = new List<int>();

        /// <summary>The rows being built, before they are published in one go.</summary>
        readonly List<StatusEntry> _gathered = new List<StatusEntry>();

        StatusListComponent _list;

        public SystemsPage(ReactorEditor editor) : base(editor)
        {
        }

        /// <summary>
        /// What the construct is called, with what this screen is about led in front of it.
        /// </summary>
        /// <remarks>
        /// It said "Your ship" for a long time, which made two claims and could be wrong about both: not
        /// every construct is a ship, and on a server it is the faction's rather than yours. Worse, it
        /// said the same thing on every Reactor in the game, so the one screen somebody arrives on never
        /// told them where they had arrived.
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

            var start = new StartComponent();
            start.Add("New system", "What is it?", Editor.RegisterText(Cmd.NewSystem, Make, Free));
            start.Add("Load system", Editor.Register(Cmd.LoadSystem, delegate { Editor.OfferLoading(); }));
            components.Add(start);

            _list = new StatusListComponent();
            _list.Fills = true;
            _list.EmptyText = "Nothing automated here yet.";

            _rowOwners.Clear();
            _rowInstances.Clear();
            _rowFitted.Clear();

            // Gathered and published in one go. This list says when it changes, and anything listening
            // re-reads the whole of it when it does - so adding row by row announced a half-built list
            // once per row, and had every listener walk every row each time.
            _gathered.Clear();

            for (var i = 0; i < _systems.Count; i++)
            {
                var system = _systems[i];

                // The system's own row does not open the editor. Editing happens through one
                // of them, where there are real blocks to point at rather than roles to
                // imagine. What this row should open - what the system is, its description,
                // giving it away - is not decided.
                var heading = new StatusEntry(SystemSummary.Describe(Editor.Runner.Bindings, system), 0);
                heading.SwitchCommand = Editor.RegisterToggle(Cmd.Enable, system.Id, Switch(system));
                heading.SwitchIsOn = system.Enabled;
                heading.SwitchGovernsAll = true;
                _gathered.Add(heading);
                _rowOwners.Add(system);
                _rowInstances.Add(null);
                _rowFitted.Add(-1);

                var instances = system.Instances;
                for (var n = 0; n < instances.Count; n++)
                {
                    var instance = instances[n];
                    var entry = new StatusEntry(
                        SystemSummary.DescribeInstance(Editor.Runner.Bindings, system, instance),
                        Editor.Register(Cmd.OpenSystem, system.Id, instance.Number,
                            delegate { Editor.Push(new SystemPage(Editor, system)); }));
                    entry.Depth = 1;

                    // Stopping one of a system without stopping the rest, which is what
                    // somebody welding on the port airlock actually wants.
                    entry.SwitchCommand = Editor.RegisterToggle(
                        Cmd.EnableInstance, system.Id, instance.Number, Switch(instance));
                    entry.SwitchIsOn = instance.Enabled;

                    _gathered.Add(entry);
                    _rowOwners.Add(system);
                    _rowInstances.Add(instance);
                    _rowFitted.Add(-1);
                }

                var another = new StatusEntry("add",
                    Editor.Register(Cmd.AddInstance, system.Id,
                        delegate { Editor.OfferAnother(system); }));
                another.Depth = 1;
                another.IsAction = true;
                _gathered.Add(another);
                _rowOwners.Add(system);
                _rowInstances.Add(null);
                _rowFitted.Add(-1);
            }

            _list.Entries.Reset(_gathered);
            components.Add(_list);
        }

        /// <summary>
        /// Makes a system, once somebody has said what it is.
        /// </summary>
        /// <remarks>
        /// The name is what M says when something is stuck, what J navigates by and what K
        /// sends to a friend. Every attempt at working it out instead - block prefixes,
        /// numbers - produced something worse than the question, and asking an adult what they
        /// are building is not an imposition. Guessing badly on their behalf is.
        /// </remarks>
        /// <summary>
        /// Stopping a system from the list it appears in.
        /// </summary>
        /// <remarks>
        /// The same switch its own screen carries. Somebody who wants a hangar left alone
        /// while they weld should not have to open it to say so - and having stopped it, the
        /// list is where they will look to check.
        /// </remarks>
        Action<bool> Switch(AutomationSystem system)
        {
            return delegate(bool on)
            {
                system.Enabled = on;
                Editor.Invalidate();
            };
        }

        /// <summary>Whether no system by this author is called this yet.</summary>
        bool Free(string name)
        {
            return Editor.Program.FindSystem(Editor.Author, Naming.Clean(name)) == null;
        }

        static Action<bool> Switch(SystemInstance instance)
        {
            return delegate(bool on) { instance.Enabled = on; };
        }

        void Make(string name)
        {
            name = Naming.Clean(name);
            if (name.Length == 0 || !Free(name))
                return;

            var system = Editor.Program.AddSystem();
            system.Namespace = Editor.Author;
            system.Name = name;

            // Straight to the system, and no further. Its screen is where the ways to start
            // a sequence live, so there is nothing left to ask on the way.
            Editor.Push(new SystemPage(Editor, system));
        }

        public override void Refresh()
        {
            var before = Rows();
            Collect();
            if (Rows() != before)
            {
                Editor.Invalidate();
                return;
            }

            if (_list == null)
                return;

            var recount = SecondPassed();
            var entries = _list.Entries;
            var count = entries.Count < _rowOwners.Count ? entries.Count : _rowOwners.Count;

            for (var i = 0; i < count; i++)
            {
                var entry = entries[i];
                var system = _rowOwners[i];

                if (entry.IsAction)
                    continue;

                // Only one of a system is ever marked, never the system itself. The system is not
                // what is broken and not what somebody opens to mend it, and marking both says
                // there are two problems.
                if (recount && _rowInstances[i] != null)
                    entry.Problems = SystemSummary.Problems(
                        Editor.World, Editor.Runner.Bindings, Editor.Runner, system,
                        _rowInstances[i]);

                if (_rowInstances[i] == null)
                {
                    if (_rowFitted[i] != system.Instances.Count)
                    {
                        _rowFitted[i] = system.Instances.Count;
                        entry.Status = SystemSummary.Count(system);
                    }

                    entry.SwitchIsOn = system.Enabled;
                    continue;
                }

                entry.SwitchIsOn = _rowInstances[i].Enabled;
                entry.Status = SystemSummary.RunningText(SystemSummary.IsRunning(system, Editor.Runner));
            }
        }

        int Rows()
        {
            var rows = 0;
            for (var i = 0; i < _systems.Count; i++)
                rows += 2 + _systems[i].Instances.Count;
            return rows;
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
