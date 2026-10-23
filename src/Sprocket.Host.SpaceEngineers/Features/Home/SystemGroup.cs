using Sprocket.Host.SpaceEngineers.Features.Codex;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    /// <summary>A logic system's heading on the home page, its installations, and the way to add another.</summary>
    public partial class SystemGroup : VStack
    {
        readonly SprocketEditor _editor;
        readonly HomePage _page;
        readonly AutomationSystem _system;

        readonly Arguments _named = new Arguments();

        readonly List<SystemInstance> _keys = new List<SystemInstance>();

        readonly KeyedChildren<SystemInstance, StatusRow> _rows;

        readonly ICommand _takesOff;

        readonly ICommand _deletes;

        readonly ICommand _shelves;
        readonly ICommand _renews;
        readonly ICommand _chooses;

        bool _waiting;
        string _installationsWord = "";
        bool _linksTerm = true;

        /// <summary>The group of <paramref name="system" /> on <paramref name="page" />, pressing through <paramref name="editor" />.</summary>
        public SystemGroup(SprocketEditor editor, HomePage page, AutomationSystem system)
        {
            _editor = editor;
            _page = page;
            _system = system;

            CreateView();
            _heading.Opens = editor.Act(CommandKind.OpenSystem, () => page.Open(system));
            _heading.Flips = editor.Act(CommandKind.EnableSystem, () => editor.SwitchSystem(system, !system.Enabled));
            _heading.SwitchGovernsAll = true;
            _heading.Mark = Symbols.LogicSystem;
            _takesOff = editor.Act(CommandKind.TakeOff, () => editor.Holdings.AskTakeOff(system));
            _deletes = editor.Act(CommandKind.DeleteSystem, () => editor.DeleteSystem(system));
            _renews = editor.Act(CommandKind.Renew, () => editor.Holdings.AskRenew(system));
            _chooses = editor.Act(CommandKind.ChooseVersion, () => editor.Versions.AskChoose(system));
            _shelves = editor.Act(CommandKind.Shelve, delegate
            {
                if (editor.Holdings.Stored(system))
                {
                    editor.Holdings.AskUpdate(system);
                    return;
                }

                editor.Holdings.Shelve(system);
                editor.OpenLibrary(system.Identity);
            });
            _heading.AboutArguments = _named;
            _another.Command = editor.Act(CommandKind.AddInstance, () => page.Another(system));
            editor.Codex.Explain(_installations);
            _rows = new KeyedChildren<SystemInstance, StatusRow>(_list.Children, Made);
        }

        /// <summary>The row over the installations of the logic system, which opens it.</summary>
        public StatusRow Heading => _heading;

        /// <summary>The line under the installations that adds another.</summary>
        public Button Adds => _another;

        /// <summary>Puts a row in place for each installation of the logic system, and gives everything its words.</summary>
        public void Arrange(Texts texts)
        {
            _named.Set("name", _system.Name);
            _heading.Summary = _system.Name;
            _heading.About = texts.Codex.SystemsEditBlueprint;

            var stored = _editor.Holdings.Stored(_system);
            _heading.BinMark = stored ? Symbols.Minus : null;
            _heading.Deletes = stored ? _takesOff : _deletes;
            _heading.DeleteText = stored ? texts.Codex.LibraryTakeOff : texts.Codex.LibraryDeletes;

            var shelf = _editor.Shelf;
            var newer = stored && !_editor.Holdings.ChangedSince(_system);
            _heading.Offers = shelf == null || stored && shelf.Holds(_system) ? null : newer ? _renews : _shelves;
            _heading.ButtonText = !stored ? texts.SystemsMakeBlueprint : newer ? texts.SystemsUpdateFromLibrary : texts.SystemsUpdateBlueprint;
            _heading.ButtonAbout = !stored ? texts.Codex.SystemsMakeBlueprintAbout
                : newer ? texts.Codex.SystemsUpdateFromLibraryAbout : texts.Codex.SystemsUpdateBlueprintAbout;

            // A version comes or goes only with a change that arranges the group again.
            _waiting = _editor.Versions.Rival(_system) != null;
            if (_waiting)
            {
                _heading.Offers = _chooses;
                _heading.ButtonText = texts.SystemsChooseVersion;
                _heading.ButtonAbout = Template.Fill(texts.Codex.SystemsChooseVersionAbout, "name", _system.Name);
            }

            _keys.Clear();
            var instances = _system.Instances;
            for (var n = 0; n < instances.Count; n++)
                _keys.Add(instances[n]);

            _rows.Show(_keys);

            // A logic system keeps at least one installation.
            var only = instances.Count == 1;
            for (var n = 0; n < instances.Count; n++)
            {
                var instance = instances[n];
                var row = _rows[instance];
                row.Summary = SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, _system, instance);
                row.About = texts.Codex.SystemsOpenInstanceAbout;
                row.AboutArguments.Set("name", row.Summary);
                row.Deletes = only ? null : _editor.Act(CommandKind.DeleteInstance, () => _editor.DeleteInstance(_system, instance));
                row.DeleteText = texts.Codex.SystemDeleteInstallation;
                row.Locked = only ? texts.Codex.SystemsKeepsOne : "";
            }

            _installationsWord = texts.SystemsInstallations.ToUpperInvariant();
            HeadInstallations();
            _another.Text = StatusRow.Plus + texts.SystemsAddAnother;
            _another.Tip = Tip.For(_another.Tip, texts.Codex.SystemsAddAnotherAbout, _named);
        }

        /// <summary>Whether the heading over the installations is the term, explained by the codex's page on it: only the first shown on the screen is, as a page explains a term once.</summary>
        public bool LinksTerm
        {
            get { return _linksTerm; }
            set
            {
                if (_linksTerm == value)
                    return;

                _linksTerm = value;
                HeadInstallations();
            }
        }

        /// <summary>Gives the heading over the installations its words, the term the codex explains where <see cref="LinksTerm" /> says so.</summary>
        void HeadInstallations()
        {
            _installations.Markdown = _linksTerm ? CodexPages.Link(_installationsWord, CodexPages.Installation) : Markdown.Escape(_installationsWord);
        }

        /// <summary>
        /// Shows the group where the logic system's name or an installation's holds <paramref name="words" />, as every search box
        /// matches them: every installation where the logic system's name does, and otherwise only those whose own names do.
        /// </summary>
        public void Narrow(string words)
        {
            var named = EntityQuery.MatchesText(_system.Name, words);
            var any = false;
            foreach (var instance in _system.Instances)
            {
                var row = _rows[instance];
                row.IsVisible = named || EntityQuery.MatchesText(row.Summary, words);
                any |= row.IsVisible;
            }

            IsVisible = named || any;
        }

        /// <summary>Brings each row up to what its installation is doing now, and the heading up to the loudest of them.</summary>
        public void Live(bool recount, bool shown, Texts texts)
        {
            var instances = _system.Instances;
            for (var n = 0; n < instances.Count; n++)
                _page.Live(_rows[instances[n]], _system, instances[n], recount, shown, texts);

            Loudest(shown, texts);
        }

        /// <summary>
        /// Marks the heading as the installations of the logic system are: broken where any is, warned where any is, running where
        /// any is; and its switch as the logic system's.
        /// </summary>
        /// <remarks>The rows must have just been brought up to date.</remarks>
        void Loudest(bool shown, Texts texts)
        {
            var problems = 0;
            var remarks = 0;
            var parked = false;
            var running = false;

            var instances = _system.Instances;
            for (var n = 0; n < instances.Count; n++)
            {
                var row = _rows[instances[n]];
                problems += row.Problems;
                remarks += row.Remarks;
                parked |= row.Parked;
                running |= row.Running;
            }

            if (_waiting)
                problems++;

            var on = _system.Enabled;
            if (!shown && _heading.Problems == problems && _heading.Remarks == remarks && _heading.Parked == parked
                && _heading.Running == running && _heading.SwitchIsOn == on)
                return;

            _heading.SwitchIsOn = on;
            _heading.SwitchAbout = _editor.SwitchAbout(_system);
            _heading.Problems = problems;
            _heading.Remarks = remarks;
            _heading.Parked = parked;
            _heading.Running = running;
            _heading.Status = SystemSummary.RunningText(texts, running);
            _heading.Show(texts);
        }

        /// <summary>The row of <paramref name="instance" />, one of the installations of the logic system.</summary>
        StatusRow Made(SystemInstance instance)
        {
            var row = new StatusRow
            {
                Mark = Symbols.Installation,
                Depth = 1,
                Opens = _editor.Act(CommandKind.OpenSystem, () => _page.Open(_system, instance)),
                AboutArguments = new Arguments()
            };

            _page.Switched(row, _system, instance);
            return row;
        }
    }
}
