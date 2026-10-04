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
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    /// <summary>
    /// The logic systems on this construct, each with its installations. The front door.
    /// </summary>
    /// <remarks>
    /// Not a list of sequences. Nobody built a procedure - they built an airlock, and that is what they
    /// came back to look at.
    /// <para>
    /// A logic system is written against slots, and each installation says which blocks fill them, from the moment it
    /// is made: the logic and where it is installed are two things from the first minute, so a second installation is
    /// one more row and nothing changes shape.
    /// </para>
    /// <para>
    /// The elements it is drawn with are declared in <c>HomePage.View.cs</c>, and nothing else is; this
    /// half is what it knows, what it says and what it does.
    /// </para>
    /// </remarks>
    public sealed partial class HomePage : EditorPage
    {
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();

        int _shape;

        Keyed<AutomationSystem, SystemGroup> _groups;

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

        /// <inheritdoc />
        public override string CustomName => Editor.World.Name;

        /// <summary>The construct's own history: logic systems and installations made and taken out.</summary>
        public override bool HasHistory => true;

        /// <summary>Not for a copy lent to look at, which changes nothing.</summary>
        public override bool CanUndo => Editor.Holder == null && Editor.CanUndoConstruct();

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabelConstruct();

        /// <inheritdoc />
        public override void Undo()
        {
            Editor.UndoConstruct();
        }

        /// <inheritdoc />
        public override bool CanRedo => Editor.Holder == null && Editor.CanRedoConstruct();

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabelConstruct();

        /// <inheritdoc />
        public override void Redo()
        {
            Editor.RedoConstruct();
        }

        /// <inheritdoc />
        public override ICommand Library => Editor.Act(Cmd.LoadSystem, Editor.OpenLibrary);

        /// <summary>
        /// Reaching outside the automation to rename the machine itself, which is deliberate.
        /// </summary>
        /// <remarks>
        /// It is the one thing on this screen that is not automation, and it is here because the name is
        /// already here and being read. Somebody looking at a construct called "Large Grid 8321" and
        /// wanting it called something else should not have to leave, find the terminal, and come back.
        /// </remarks>
        public override bool CanRename => Editor.World.CanRename;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.SystemsNameAbout;

        /// <inheritdoc />
        public override bool NameRequired => true;

        /// <inheritdoc />
        public override void Rename(string name)
        {
            var wanted = (name ?? "").Trim();
            if (wanted.Length == 0)
                return;

            Editor.World.Rename(wanted);
        }

        /// <inheritdoc />
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
            Installed();
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
            if (reshaped)
            {
                Installed();
                shape = Shape();
            }

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

        /// <summary>Gives the button its press, keeps the groups by system, and fills the view from the systems as they are.</summary>
        protected override void ViewCreated()
        {
            _new.Command = Editor.Act(Cmd.NewSystem, Make);
            _groups = new Keyed<AutomationSystem, SystemGroup>(_list.Children, system => new SystemGroup(Editor, this, system));

            Show(true);
        }

        /// <summary>Brings the view up to the systems, and to what they are called where <paramref name="reshaped" />; nothing before the view is made.</summary>
        void Show(bool reshaped)
        {
            if (_groups == null)
                return;

            var texts = Editor.Texts;

            // Counted when the rows are new as well, so a broken system is marked from the first frame.
            var recount = SecondPassed() || reshaped;

            if (reshaped)
                Arrange(texts);

            for (var i = 0; i < _systems.Count; i++)
                _groups[_systems[i]].Live(recount, reshaped, texts);
        }

        /// <summary>Puts a group in place for each logic system, and gives everything its words.</summary>
        /// <remarks>Walks the systems' blocks for their names, which is why it runs only when the systems change or the page is come back to.</remarks>
        void Arrange(Texts texts)
        {
            _new.Text = StatusRow.Plus + texts.SystemsNew;
            _new.Tip = Tip.For(_new.Tip, texts.SystemsNewAbout);

            _groups.Show(_systems);

            _heading.Text = texts.SystemsHeading.ToUpperInvariant();
            _empty.IsVisible = _systems.Count == 0;
            _empty.Text = texts.SystemsEmpty;

            for (var i = 0; i < _systems.Count; i++)
                _groups[_systems[i]].Arrange(texts);
        }

        /// <summary>Makes a logic system, numbered, with its first installation, and opens it with its name in hand to be given.</summary>
        void Make()
        {
            var system = Editor.Program.AddSystem(Editor.Texts.SystemsDefault);
            system.Namespace = Editor.Author;
            system.Enabled = false;
            Editor.Blueprinted(system);
            Editor.Do(new MakeSystem(system));

            Editor.PushMade(new LogicSystemPage(Editor, system));
        }

        /// <summary>Gives every logic system on the construct that lacks them its slots and a first installation.</summary>
        /// <remarks>The editor shows only the one shape, so a logic system is given that shape when the construct's list sees it.</remarks>
        void Installed()
        {
            var systems = Editor.Program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                if (!systems[i].IsBlueprint || systems[i].Instances.Count == 0)
                    Editor.Installed(systems[i]);
            }
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
