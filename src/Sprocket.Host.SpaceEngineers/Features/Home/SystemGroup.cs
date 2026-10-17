using Sprocket.Host.SpaceEngineers.Features.Codex;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    /// <summary>A logic system's heading on the home page, its installations, and the way to add another.</summary>
    /// <remarks>The elements it is drawn with are declared in <c>SystemGroup.View.cs</c>, and nothing else is.</remarks>
    public partial class SystemGroup : VStack
    {
        readonly SprocketEditor _editor;
        readonly HomePage _page;
        readonly AutomationSystem _system;

        readonly Arguments _named = new Arguments();

        readonly List<SystemInstance> _keys = new List<SystemInstance>();

        readonly Keyed<SystemInstance, StatusRow> _rows;

        readonly ICommand _takesOff;

        readonly ICommand _deletes;

        readonly ICommand _shelves;
        readonly ICommand _renews;
        readonly ICommand _chooses;

        bool _waiting;

        /// <summary>The group of <paramref name="system" /> on <paramref name="page" />, pressing through <paramref name="editor" />.</summary>
        public SystemGroup(SprocketEditor editor, HomePage page, AutomationSystem system)
        {
            _editor = editor;
            _page = page;
            _system = system;

            CreateView();
            _heading.Opens = editor.Act(Cmd.OpenSystem, () => page.Open(system));
            _heading.Flips = editor.Act(Cmd.EnableSystem, () => editor.SwitchSystem(system, !system.Enabled));
            _heading.SwitchGovernsAll = true;
            _heading.Mark = Symbols.LogicSystem;
            _takesOff = editor.Act(Cmd.TakeOff, () => editor.Holdings.AskTakeOff(system));
            _deletes = editor.Act(Cmd.DeleteSystem, () => editor.DeleteSystem(system));
            _renews = editor.Act(Cmd.Renew, () => editor.Holdings.AskRenew(system));
            _chooses = editor.Act(Cmd.ChooseVersion, () => editor.Versions.AskChoose(system));
            _shelves = editor.Act(Cmd.Shelve, delegate
            {
                // Updating replaces the blueprint, so it asks first; making one shows it where it now is.
                if (editor.Holdings.Stored(system))
                {
                    editor.Holdings.AskUpdate(system);
                    return;
                }

                editor.Holdings.Shelve(system);
                editor.OpenLibrary(system.Identity);
            });
            _heading.AboutArguments = _named;
            _another.Command = editor.Act(Cmd.AddInstance, () => page.Another(system));
            editor.Codex.Wire(_installations, editor.OpenCodex);
            _rows = new Keyed<SystemInstance, StatusRow>(_list.Children, Made);
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
            _heading.About = texts.SystemsEditBlueprint;

            // A logic system with a blueprint in the library is taken off the construct, with a minus; one without is deleted,
            // with a bin, since this is its only copy. Its installations go with it either way.
            var stored = _editor.Holdings.Stored(_system);
            _heading.BinMark = stored ? Symbols.Minus : null;
            _heading.Deletes = stored ? _takesOff : _deletes;
            _heading.DeleteText = stored ? texts.LibraryTakeOff : texts.LibraryDeletes;

            // The library's own button, here as well: the blueprint is the logic system's copy in the library, made the first
            // time and brought up to date after. Where the blueprint was changed after the logic system, the logic system is
            // brought up to date from it instead, and with the two the same there is nothing to offer.
            var shelf = _editor.Shelf;
            var newer = stored && !_editor.Holdings.ChangedSince(_system);
            _heading.Offers = shelf == null || stored && shelf.Holds(_system) ? null : newer ? _renews : _shelves;
            _heading.ButtonText = !stored ? texts.SystemsMakeBlueprint : newer ? texts.SystemsUpdateFromLibrary : texts.SystemsUpdateBlueprint;
            _heading.ButtonAbout = !stored ? texts.SystemsMakeBlueprintAbout
                : newer ? texts.SystemsUpdateFromLibraryAbout : texts.SystemsUpdateBlueprintAbout;

            // Kept from here: a version comes or goes only with a change that arranges the group again.
            _waiting = _editor.Versions.Rival(_system) != null;
            if (_waiting)
            {
                _heading.Offers = _chooses;
                _heading.ButtonText = texts.SystemsChooseVersion;
                _heading.ButtonAbout = Template.Fill(texts.SystemsChooseVersionAbout, "name", _system.Name);
            }

            _keys.Clear();
            var instances = _system.Instances;
            for (var n = 0; n < instances.Count; n++)
                _keys.Add(instances[n]);

            _rows.Show(_keys);

            // A logic system keeps at least one installation, so the only one's bin says so rather than deleting it.
            var only = instances.Count == 1;
            for (var n = 0; n < instances.Count; n++)
            {
                var instance = instances[n];
                var row = _rows[instance];
                row.Summary = SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, _system, instance);
                row.About = texts.SystemsOpenInstanceAbout;
                row.AboutArguments.Set("name", row.Summary);
                row.Deletes = only ? null : _editor.Act(Cmd.DeleteInstance, () => _editor.DeleteInstance(_system, instance));
                row.DeleteText = texts.SystemDeleteInstallation;
                row.Locked = only ? texts.SystemsKeepsOne : "";
            }

            _installations.Markdown = Terms.Link(texts.SystemsInstallations.ToUpperInvariant(), Terms.Installation);
            _another.Text = StatusRow.Plus + texts.SystemsAddAnother;
            _another.Tip = Tip.For(_another.Tip, texts.SystemsAddAnotherAbout, _named);
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
        /// <remarks>Read from the rows below, which have just been brought up to date, so nothing is asked of the runner twice.</remarks>
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
                Opens = _editor.Act(Cmd.OpenSystem, () => _page.Open(_system, instance)),
                AboutArguments = new Arguments()
            };

            _page.Switched(row, _system, instance);
            return row;
        }
    }
}
