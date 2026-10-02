using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Lumel;
using Lumel.Input;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Home
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
    /// <para>
    /// The elements it is drawn with are declared in <c>HomePage.View.cs</c>, and nothing else is; this
    /// half is what it knows, what it says and what it does.
    /// </para>
    /// </remarks>
    sealed partial class HomePage : EditorPage
    {
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();

        readonly List<AutomationSystem> _own = new List<AutomationSystem>();
        readonly List<AutomationSystem> _blueprints = new List<AutomationSystem>();

        int _shape;

        Keyed<AutomationSystem, StatusRow> _rows;
        Keyed<AutomationSystem, BlueprintGroup> _groups;

        /// <summary>The root of the editor built on <paramref name="editor" />.</summary>
        public HomePage(SprocketEditor editor) : base(editor)
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

        /// <summary>Which of the logic systems nothing starts is read from the toolbars.</summary>
        public override bool CanRefresh => true;

        /// <inheritdoc />
        public override ICommand Library => Editor.Act(Cmd.LoadSystem, Editor.OfferLoading);

        /// <summary>
        /// Reaching outside the automation to rename the machine itself, which is deliberate.
        /// </summary>
        /// <remarks>
        /// It is the one thing on this screen that is not automation, and it is here because the name is
        /// already here and being read. Somebody looking at a construct called "Large Grid 8321" and
        /// wanting it called something else should not have to leave, find the terminal, and come back.
        /// </remarks>
        public override bool CanRename => Editor.World.CanRename;

        public override string NameAbout => Editor.Texts.SystemsNameAbout;

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

            // One sentence, so a translation can put words on either side of the name. The name is the
            // heading's own, pressed to rename the grid, so the words are drawn around it.
            const string slot = "{name}";
            var title = Editor.Texts.SystemsTitle;
            var at = title.IndexOf(slot, StringComparison.Ordinal);

            parts[0].Lead = (at < 0 ? title : title.Substring(0, at)).TrimEnd();
            parts[0].After = at < 0 ? "" : title.Substring(at + slot.Length).TrimStart();
        }

        /// <summary>Brings the view up to date, names and all.</summary>
        /// <remarks>Called when the editor builds the screen: on arriving at the page, coming back to it, and a change of language.</remarks>
        public override void Build()
        {
            // Arriving at the construct's own list is the moment somebody wants to know how everything on it stands.
            Editor.Runner.Look();
            Collect();
            _shape = Shape();
            Show(true);
        }

        /// <summary>Brings the view up to what the systems are doing, and to what systems there are when that has changed.</summary>
        /// <remarks>
        /// Every tick the page is showing. Collecting the systems and comparing their shape is a pass over a
        /// short list; the rows write only what changed, and the problems are counted once a second.
        /// </remarks>
        public override void Refresh()
        {
            Collect();

            var shape = Shape();
            var reshaped = shape != _shape;
            _shape = shape;

            Show(reshaped);
        }

        /// <summary>Gives a row the switch of one of the logic systems made from a blueprint, which stops that one and nothing else.</summary>
        /// <remarks>It shows its own setting, and cannot be pressed while the blueprint is off - see <see cref="Live(StatusRow, AutomationSystem, SystemInstance, bool, bool, Texts)" />.</remarks>
        public void Switched(StatusRow row, AutomationSystem system, SystemInstance instance)
        {
            if (instance == null)
                return;

            row.Flips = Editor.Act(Cmd.EnableInstance, delegate
            {
                Editor.SwitchInstance(system, instance, !instance.Enabled);
            });
        }

        /// <summary>
        /// Brings a logic system's row up to what it is doing now, and shows it where anything changed.
        /// </summary>
        /// <remarks>
        /// This logic system, not the blueprint: with two airlocks, the port one cycling is not the front one
        /// cycling. The ground says it as well as the word, the same way a row leading to something broken is
        /// washed rather than only marked.
        /// </remarks>
        public void Live(StatusRow row, AutomationSystem system, SystemInstance instance, bool recount, bool shown,
            Texts texts)
        {
            Live(row, system, instance, instance != null && system.Enabled && instance.Enabled, instance == null ? null : instance.Halt,
                recount, shown, texts);
        }

        /// <summary>Brings a row up to what its logic system is doing now, <paramref name="on" /> saying whether it may run, and why Sprocket switched it off where it did.</summary>
        void Live(StatusRow row, AutomationSystem system, SystemInstance instance, bool on, Halt halt, bool recount, bool shown,
            Texts texts)
        {
            var changed = shown;

            if (instance != null)
            {
                if (recount)
                {
                    var problems = SystemSummary.Problems(Editor.Runner, system, instance);
                    var remarks = SystemSummary.Remarks(Editor.Runner, system, instance);
                    var stopped = on || halt == null ? "" : SequenceText.Stopped(texts, halt, system);
                    var said = stopped.Length > 0 || problems == 0 ? stopped : Failing(system, instance);
                    changed |= row.Problems != problems || row.Remarks != remarks || row.Problem != said;
                    row.Problems = problems;
                    row.Remarks = remarks;
                    row.Problem = said;
                    row.ProblemIsWarning = problems == 0;
                    row.Parked = stopped.Length > 0;
                }

                // An installation's switch shows its own setting, and waits for the blueprint's while that is off, so
                // switching the blueprint back on starts exactly the ones that were on. A plain system's is the system's.
                var installed = system.IsBlueprint;
                var own = installed ? instance.Enabled : on;
                var held = installed && !system.Enabled ? texts.RowSwitchBlueprintFirst : null;
                var going = SystemSummary.IsRunning(system, Editor.Runner, instance);
                changed |= row.SwitchIsOn != own || row.SwitchLocked != held || row.Running != going;
                row.SwitchIsOn = own;
                row.SwitchLocked = held;
                row.Running = going;
                row.Status = SystemSummary.RunningText(texts, going);
            }

            if (changed)
                row.Show(texts);
        }

        /// <summary>What is wrong with <paramref name="system" /> on <paramref name="instance" />, said in place of what it is doing.</summary>
        string Failing(AutomationSystem system, SystemInstance instance)
        {
            SequenceFault fault;
            var sequence = SystemSummary.Failing(Editor.Runner, system, instance, out fault);
            return sequence == null ? "" : SequenceText.Failing(Editor.Texts, fault, sequence);
        }

        /// <summary>Adds another logic system made from a blueprint, numbered, and opens its configuration panel on it with its name in hand to be given.</summary>
        public void Another(AutomationSystem system)
        {
            if (Editor.Another(system) != null)
                Editor.PushMade(new BlueprintPage(Editor, system, false));
        }

        /// <summary>Opens the blueprint on one of the logic systems made from it.</summary>
        public void Open(AutomationSystem system, SystemInstance instance)
        {
            Editor.Choose(system, instance);
            Editor.Push(new BlueprintPage(Editor, system, false));
        }

        /// <summary>Gives the two buttons their presses, keeps the rows and groups by system, and fills the view from the systems as they are.</summary>
        protected override void ViewCreated()
        {
            _new.Command = Editor.Act(Cmd.NewSystem, Make);

            _rows = new Keyed<AutomationSystem, StatusRow>(_plainRows.Children, Own);
            _groups = new Keyed<AutomationSystem, BlueprintGroup>(_blueprintGroups.Children, system => new BlueprintGroup(Editor, this, system));

            Show(true);
        }

        /// <summary>Brings the view up to the systems, and to what they are called where <paramref name="reshaped" />; nothing before the view is made.</summary>
        void Show(bool reshaped)
        {
            if (_rows == null)
                return;

            var texts = Editor.Texts;

            // Counted when the rows are new as well, so a broken system is marked from the first frame.
            var recount = SecondPassed() || reshaped;

            if (reshaped)
                Arrange(texts);

            for (var i = 0; i < _own.Count; i++)
            {
                var system = _own[i];
                Live(_rows[system], system, system.First, system.Enabled, system.Halt, recount, reshaped, texts);
            }

            for (var i = 0; i < _blueprints.Count; i++)
                _groups[_blueprints[i]].Live(recount, reshaped, texts);
        }

        /// <summary>Puts a row or a group in place for each system, and gives everything its words.</summary>
        /// <remarks>Walks the systems' blocks for their names, which is why it runs only when the systems change or the page is come back to.</remarks>
        void Arrange(Texts texts)
        {
            _new.Text = StatusRow.Plus + texts.SystemsNew;
            _new.Tip = Tip.For(_new.Tip, texts.SystemsNewAbout);

            _own.Clear();
            _blueprints.Clear();
            for (var i = 0; i < _systems.Count; i++)
                (_systems[i].IsBlueprint ? _blueprints : _own).Add(_systems[i]);

            _rows.Show(_own);
            _groups.Show(_blueprints);

            _plainHeading.Text = texts.SystemsPlainHeading.ToUpperInvariant();
            _blueprintsHeading.Text = texts.SystemsBlueprintsHeading.ToUpperInvariant();
            _empty.IsVisible = _systems.Count == 0;
            _empty.Text = texts.SystemsEmpty;

            for (var i = 0; i < _own.Count; i++)
            {
                var row = _rows[_own[i]];
                row.Summary = SystemSummary.Describe(texts, Editor.Runner.Bindings, _own[i]);

                // Said in words on the row, where it cannot be missed. Making a blueprint is how one logic
                // system becomes several, and a way to do that nobody finds is the problem this row solves.
                row.ButtonText = texts.SystemsMakeBlueprint;
                row.ButtonAbout = texts.SystemsMakeBlueprintAbout;

                row.About = texts.SystemsOpenAbout;
                row.AboutArguments.Set("name", row.Summary);
                row.DeleteText = texts.SystemDelete;
            }

            for (var i = 0; i < _blueprints.Count; i++)
                _groups[_blueprints[i]].Arrange(texts);
        }

        /// <summary>The row of a logic system of its own: opened by pressing it, switched by its switch, and made a blueprint of by its button.</summary>
        StatusRow Own(AutomationSystem system)
        {
            return new StatusRow
            {
                Opens = Editor.Act(Cmd.OpenSystem, delegate { Open(system); }),
                Flips = Editor.Act(Cmd.EnableSystem, delegate { Editor.SwitchSystem(system, !system.Enabled); }),
                Offers = Editor.Act(Cmd.MakeBlueprint, delegate { Made(system); }),
                Deletes = Editor.Act(Cmd.DeleteSystem, delegate { Editor.DeleteSystem(system); }),
                AboutArguments = new Arguments(),
                SwitchGovernsAll = true
            };
        }

        /// <summary>Makes a logic system, numbered, and opens it with its name in hand to be given.</summary>
        void Make()
        {
            var system = Editor.Program.AddSystem(Editor.Texts.SystemsDefault);
            system.Namespace = Editor.Author;
            system.Enabled = false;

            Editor.PushMade(new LogicSystemPage(Editor, system));
        }

        /// <summary>Makes a blueprint of a logic system, and opens it to be written: the blueprint keeps the system's name, and so does the first logic system made from it.</summary>
        void Made(AutomationSystem system)
        {
            var first = Editor.Blueprinted(system);
            if (first == null)
                return;

            first.Name = system.Name;
            Editor.Choose(system, first);
            Editor.Push(new BlueprintPage(Editor, system, true));
        }

        /// <summary>Opens what <paramref name="system" /> does, on the logic system screen.</summary>
        public void Open(AutomationSystem system)
        {
            Editor.Push(new LogicSystemPage(Editor, system));
        }

        /// <summary>What the page is shown from, as one number: which systems, how many of each, and which are blueprints.</summary>
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
