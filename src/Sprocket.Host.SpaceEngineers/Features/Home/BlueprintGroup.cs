using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    /// <summary>A blueprint's heading on the home page, its installations, and the way to add another.</summary>
    /// <remarks>The elements it is drawn with are declared in <c>BlueprintGroup.View.cs</c>, and nothing else is.</remarks>
    partial class BlueprintGroup : VStack
    {
        readonly SprocketEditor _editor;
        readonly HomePage _page;
        readonly AutomationSystem _system;

        readonly Arguments _named = new Arguments();

        readonly List<SystemInstance> _keys = new List<SystemInstance>();

        readonly Keyed<SystemInstance, StatusRow> _rows;

        /// <summary>The group of <paramref name="system" /> on <paramref name="page" />, pressing through <paramref name="editor" />.</summary>
        public BlueprintGroup(SprocketEditor editor, HomePage page, AutomationSystem system)
        {
            _editor = editor;
            _page = page;
            _system = system;

            CreateView();
            _heading.Opens = editor.Act(Cmd.OpenSystem, delegate { page.Open(system); });
            _heading.Mark = Symbols.Blueprint;
            _heading.AboutArguments = _named;
            _another.Command = editor.Act(Cmd.AddInstance, delegate { page.Another(system); });
            _rows = new Keyed<SystemInstance, StatusRow>(_list.Children, Made);
        }

        /// <summary>The row over the installations of the blueprint, which opens it.</summary>
        public StatusRow Heading => _heading;

        /// <summary>The line under the installations that adds another.</summary>
        public Button Adds => _another;

        /// <summary>Puts a row in place for each installation of the blueprint, and gives everything its words.</summary>
        public void Arrange(Texts texts)
        {
            _named.Set("name", _system.Name);
            _heading.Summary = _system.Name;
            _heading.About = texts.SystemsEditBlueprint;

            _keys.Clear();
            var instances = _system.Instances;
            for (var n = 0; n < instances.Count; n++)
                _keys.Add(instances[n]);

            _rows.Show(_keys);

            for (var n = 0; n < instances.Count; n++)
            {
                var row = _rows[instances[n]];
                row.Summary = SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, _system, instances[n]);
                row.About = texts.SystemsOpenInstanceAbout;
                row.AboutArguments.Set("name", row.Summary);
                row.DeleteText = texts.SystemDeleteInstallation;
            }

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

        /// <summary>Marks the heading as the installations of the blueprint are: broken where any is, warned where any is, running where any is.</summary>
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

            if (!shown && _heading.Problems == problems && _heading.Remarks == remarks && _heading.Parked == parked
                && _heading.Running == running)
                return;

            _heading.Problems = problems;
            _heading.Remarks = remarks;
            _heading.Parked = parked;
            _heading.Running = running;
            _heading.Status = SystemSummary.RunningText(texts, running);
            _heading.Show(texts);
        }

        /// <summary>The row of <paramref name="instance" />, one of the installations of the blueprint.</summary>
        StatusRow Made(SystemInstance instance)
        {
            var row = new StatusRow
            {
                Depth = 1,
                Opens = _editor.Act(Cmd.OpenSystem, delegate { _page.Open(_system, instance); }),
                Deletes = _editor.Act(Cmd.DeleteInstance, delegate { _editor.DeleteInstance(_system, instance); }),
                AboutArguments = new Arguments()
            };

            _page.Switched(row, _system, instance);
            return row;
        }
    }
}
