using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Ui;

namespace Reactor.Editing
{
    /// <summary>
    /// The machines on this ship, and the ones of each that are actually fitted. The front
    /// door.
    /// </summary>
    /// <remarks>
    /// Not a list of sequences. Nobody built a procedure - they built an airlock, and that is
    /// what they came back to look at.
    ///
    /// A machine always shows the ones of it underneath, even when there is only one. That is
    /// what makes the naming work: <c>Airlock 1</c> reads as provisional and gets renamed
    /// without a dialog asking for it, and a machine and the one of it never share a row and
    /// end up saying the same thing twice.
    /// </remarks>
    sealed class SystemsPage : EditorPage
    {
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();
        readonly List<AutomationSystem> _rowOwners = new List<AutomationSystem>();
        readonly List<SystemInstance> _rowInstances = new List<SystemInstance>();

        StatusListComponent _list;

        public SystemsPage(ReactorEditor editor) : base(editor)
        {
        }

        public override string Title { get { return "Your ship"; } }

        public override void Build(List<EditorComponent> components)
        {
            Collect();

            var start = new StartComponent();
            start.Add("New system", "What is it?", Editor.RegisterText(Cmd.NewSystem, Make));
            start.Add("Load system", Editor.Register(Cmd.LoadSystem, delegate { Editor.OfferLoading(); }));
            components.Add(start);

            _list = new StatusListComponent();
            _list.Fills = true;
            _list.EmptyText = "No systems on this ship yet.";

            _rowOwners.Clear();
            _rowInstances.Clear();

            for (var i = 0; i < _systems.Count; i++)
            {
                var system = _systems[i];

                // The machine's own row does not open the editor. Editing happens through one
                // of them, where there are real blocks to point at rather than roles to
                // imagine. What this row should open - what the machine is, its description,
                // giving it away - is not decided.
                _list.Entries.Add(new StatusEntry(SystemSummary.Describe(Editor.World, system), 0));
                _rowOwners.Add(system);
                _rowInstances.Add(null);

                var instances = system.Instances;
                for (var n = 0; n < instances.Count; n++)
                {
                    var instance = instances[n];
                    var entry = new StatusEntry(
                        SystemSummary.DescribeInstance(Editor.World, system, instance),
                        Editor.Register(Cmd.OpenSystem, system.Id, instance.Number,
                            delegate { Editor.Push(new SystemPage(Editor, system)); }));
                    entry.Depth = 1;
                    _list.Entries.Add(entry);
                    _rowOwners.Add(system);
                    _rowInstances.Add(instance);
                }

                var another = new StatusEntry("add",
                    Editor.Register(Cmd.AddInstance, system.Id,
                        delegate { Editor.OfferAnother(system); }));
                another.Depth = 1;
                another.IsAction = true;
                _list.Entries.Add(another);
                _rowOwners.Add(system);
                _rowInstances.Add(null);
            }

            components.Add(_list);
        }

        /// <summary>
        /// Makes a machine, once somebody has said what it is.
        /// </summary>
        /// <remarks>
        /// The name is what M says when something is stuck, what J navigates by and what K
        /// sends to a friend. Every attempt at working it out instead - block prefixes,
        /// numbers - produced something worse than the question, and asking an adult what they
        /// are building is not an imposition. Guessing badly on their behalf is.
        /// </remarks>
        void Make(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0)
                return;

            var system = Editor.Program.AddSystem();
            system.Name = name;

            // Straight to the machine, and no further. Its screen is where the ways to start
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

            var entries = _list.Entries;
            var count = entries.Count < _rowOwners.Count ? entries.Count : _rowOwners.Count;

            for (var i = 0; i < count; i++)
            {
                var entry = entries[i];
                var system = _rowOwners[i];

                if (entry.IsAction)
                    continue;

                if (_rowInstances[i] == null)
                {
                    entry.Status = SystemSummary.Count(system);
                    continue;
                }

                // Every one of a machine runs the same sequences, so until they point at their
                // own blocks they are all doing whatever the machine is doing.
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
