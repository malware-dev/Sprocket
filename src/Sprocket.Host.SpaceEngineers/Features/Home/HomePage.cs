using Sprocket.Host.SpaceEngineers.Features.Codex;
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
    /// <summary>The logic systems on this construct, each with its installations.</summary>
    public sealed partial class HomePage : EditorPage
    {
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();
        readonly List<string> _used = new List<string>();

        /// <summary>How wide the search box is, at the right end of the heading.</summary>
        const float SearchWide = 320f;

        int _shape;

        string _searched = "";

        KeyedChildren<AutomationSystem, SystemGroup> _groups;

        /// <summary>The root of the editor built on <paramref name="editor" />.</summary>
        public HomePage(SprocketEditor editor) : base(editor)
        {
        }

        /// <summary>The box the logic systems and installations are searched in, where the view has been made.</summary>
        public TextBox SearchBox => _search;

        /// <summary>
        /// What the construct is called, with what this screen is about led in front of it.
        /// </summary>
        public override string Title => Editor.World.Name;

        /// <inheritdoc />
        public override string CustomName => Editor.World.Name;

        /// <summary>The construct's own history: logic systems and installations made and taken out.</summary>
        public override bool HasHistory => true;

        /// <summary>Whether a change to the construct can be walked back, which a copy lent to look at never can.</summary>
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
        public override ICommand Library => Editor.Act(CommandKind.LoadSystem, Editor.OpenLibrary);

        /// <summary>Whether the construct itself can be renamed here.</summary>
        public override bool CanRename => Editor.World.CanRename;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.Codex.SystemsNameAbout;

        /// <inheritdoc />
        public override bool NameRequired => true;

        /// <inheritdoc />
        public override void Rename(string name)
        {
            var wanted = (name ?? "").Trim();
            if (wanted.Length == 0)
                return;

            Editor.Do(new RenameConstruct(Editor.World, Editor.World.Name, wanted));
        }

        /// <inheritdoc />
        public override void Heading(List<HeadingPart> parts)
        {
            base.Heading(parts);

            // One sentence, so a translation can put words on either side of the name.
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
            Editor.Runner.Look();
            Installed();
            Collect();
            _shape = Shape();
            Show(true);
        }

        /// <summary>Brings the view up to what the systems are doing, and to what systems there are when that has changed.</summary>
        /// <remarks>
        /// Every tick the page is showing: a pass over a short list, the rows writing only what changed and the problems counted
        /// once a second.
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
        public void Switched(StatusRow row, AutomationSystem system, SystemInstance instance)
        {
            if (instance == null)
                return;

            row.Flips = Editor.Act(CommandKind.EnableInstance, delegate
            {
                Editor.SwitchInstance(system, instance, !instance.Enabled);
            });
        }

        /// <summary>
        /// Brings a logic system's row up to what it is doing now, and shows it where anything changed.
        /// </summary>
        public void Live(StatusRow row, AutomationSystem system, SystemInstance instance, bool recount, bool shown,
            Texts texts)
        {
            Live(row, system, instance, instance != null && system.Enabled && instance.Enabled, instance?.Halt,
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
                    var stopped = on || halt == null ? "" : SequenceText.Stopped(texts, halt, system, problems == 0);
                    var again = system.IsBlueprint ? FillAgain(texts, system, instance) : "";
                    var said = again.Length > 0 ? again : stopped.Length > 0 ? stopped : Failing(system, instance, problems > 0);
                    changed |= row.Problems != problems || row.Remarks != remarks || row.Problem != said;
                    row.Problems = problems;
                    row.Remarks = remarks;
                    row.Problem = said;
                    row.Level = problems == 0 ? NoticeLevel.Warning : NoticeLevel.Problem;
                    row.Parked = stopped.Length > 0;
                }

                // An installation's switch keeps its own setting while the blueprint is off, so switching the blueprint on starts
                // exactly the ones that were on.
                var installed = system.IsBlueprint;
                var own = installed ? instance.Enabled : on;
                var held = installed && !system.Enabled ? texts.Codex.RowSwitchBlueprintFirst : null;
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

        /// <summary>How many of <paramref name="instance" />'s slots changed kind and hold a block that no longer fits, so are to be filled again, or empty where none.</summary>
        string FillAgain(Texts texts, AutomationSystem system, SystemInstance instance)
        {
            system.Acted(_used);
            var again = SlotMarks.Changed(Editor.Runner.Bindings, system, instance, _used);
            if (again == 0)
                return "";

            return again == 1
                ? texts.SlotFillAgainOne
                : Template.Fill(texts.SlotFillAgainMany, "count", again.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>What is wrong with <paramref name="system" /> on <paramref name="instance" /> where <paramref name="broken" />, or its first warning where not, said in place of what it is doing.</summary>
        string Failing(AutomationSystem system, SystemInstance instance, bool broken)
        {
            SequenceFault fault;
            var sequence = broken
                ? SystemSummary.Failing(Editor.Runner, system, instance, out fault)
                : SystemSummary.Warned(Editor.Runner, system, instance, out fault);
            return sequence == null ? "" : SequenceText.Failing(Editor.Texts, fault, sequence);
        }

        /// <summary>Adds another logic system made from a blueprint, numbered, and opens its installation panel on it with its name in hand to be given.</summary>
        public void Another(AutomationSystem system)
        {
            if (Editor.Another(system) != null)
                Editor.PushMade(new BlueprintPage(Editor, system));
        }

        /// <summary>Opens the blueprint on one of the logic systems made from it.</summary>
        public void Open(AutomationSystem system, SystemInstance instance)
        {
            Editor.Choose(system, instance);
            Editor.Push(new BlueprintPage(Editor, system));
        }

        /// <summary>Gives the button its press, keeps the groups by system, and fills the view from the systems as they are.</summary>
        protected override void ViewCreated()
        {
            _new.Command = Editor.Act(CommandKind.NewSystem, Make);
            _groups = new KeyedChildren<AutomationSystem, SystemGroup>(_list.Children, system => new SystemGroup(Editor, this, system));
            Editor.Codex.Explain(_heading);
            _search.Text = _searched;
            _search.Edited += delegate
            {
                _searched = _search.Text;
                Narrow();
            };

            Show(true);
        }

        /// <summary>Brings the view up to the systems, and to what they are called where <paramref name="reshaped" />; nothing before the view is made.</summary>
        void Show(bool reshaped)
        {
            if (_groups == null)
                return;

            var texts = Editor.Texts;

            // Counted when the rows are new as well, so a broken system is marked from the first frame.
            var recount = Revised() || reshaped;

            if (reshaped)
                Arrange(texts);

            foreach (var system in _systems)
                _groups[system].Live(recount, reshaped, texts);
        }

        /// <summary>Puts a group in place for each logic system, and gives everything its words.</summary>
        /// <remarks>Walks the systems' blocks for their names, so it runs only when the systems change or the page is come back to.</remarks>
        void Arrange(Texts texts)
        {
            _new.Text = StatusRow.Plus + texts.SystemsNew;
            _new.Tip = CodexTip.For(_new.Tip, texts.Codex.SystemsNewAbout);

            _groups.Show(_systems);

            _heading.Markdown = CodexPages.Term(texts.SystemsHeading.ToUpperInvariant(), CodexPages.LogicSystem);
            _empty.IsVisible = _systems.Count == 0;
            _empty.Text = texts.SystemsEmpty;
            _search.Hint = texts.PickSearch;
            // The search only where there is something to search.
            _search.IsVisible = _systems.Count != 0;

            foreach (var system in _systems)
                _groups[system].Arrange(texts);

            Narrow();
        }

        /// <summary>Shows only the logic systems and installations whose name holds the words typed in the search box, as every search box matches them, the first group shown linking its heading to the codex.</summary>
        void Narrow()
        {
            var linked = false;
            foreach (var system in _systems)
            {
                var group = _groups[system];
                group.Narrow(_searched);
                group.LinksTerm = group.IsVisible && !linked;
                linked |= group.IsVisible;
            }
        }

        /// <summary>Makes a logic system, numbered, with its first installation, and opens it with its name in hand to be given.</summary>
        void Make()
        {
            var system = Editor.Program.AddSystem(Editor.Texts.SystemsDefault);
            system.Namespace = Editor.Author;
            system.Enabled = false;
            Editor.Picks.Blueprinted(system);
            Editor.Do(new MakeSystem(system));

            Editor.PushMade(new LogicSystemPage(Editor, system));
        }

        /// <summary>Gives every logic system on the construct that lacks them its slots and a first installation.</summary>
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
            foreach (AutomationSystem system in _systems)
            {
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
