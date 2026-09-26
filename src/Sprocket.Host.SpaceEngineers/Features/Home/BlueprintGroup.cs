using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    /// <summary>A blueprint's heading on the home page, the logic systems made from it, and the way to add another.</summary>
    /// <remarks>The elements it is drawn with are declared in <c>BlueprintGroup.View.cs</c>, and nothing else is.</remarks>
    partial class BlueprintGroup : VStack
    {
        readonly SprocketEditor _editor;
        readonly HomePage _page;
        readonly AutomationSystem _system;

        readonly Arguments _named = new Arguments();

        readonly List<object> _keys = new List<object>();

        readonly Keyed<object, StatusRow> _rows;

        string _madeFrom;

        /// <summary>The group of <paramref name="system" /> on <paramref name="page" />, pressing through <paramref name="editor" />.</summary>
        public BlueprintGroup(SprocketEditor editor, HomePage page, AutomationSystem system)
        {
            _editor = editor;
            _page = page;
            _system = system;

            CreateView();
            _heading.Arguments = _named;
            _rows = new Keyed<object, StatusRow>(_list.Children, Made);
        }

        /// <summary>Puts a row in place for each logic system made from the blueprint, and gives everything its words.</summary>
        public void Arrange(Texts texts)
        {
            if (!ReferenceEquals(_madeFrom, texts.SystemsMadeFrom))
            {
                _madeFrom = texts.SystemsMadeFrom;
                _heading.Document = Template.Read(_madeFrom);
            }

            _named.Set("name", _system.Name);

            _keys.Clear();
            var instances = _system.Instances;
            for (var n = 0; n < instances.Count; n++)
                _keys.Add(instances[n]);

            _keys.Add(_system);
            _rows.Show(_keys);

            for (var n = 0; n < instances.Count; n++)
                _rows[instances[n]].Summary = SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, _system, instances[n]);

            var another = _rows[_system];
            another.Summary = Template.Fill(texts.SystemsAddAnother, "name", _system.Name);
            another.About = texts.SystemsAddAnotherAbout;
        }

        /// <summary>Brings each row up to what its logic system is doing now.</summary>
        public void Live(bool recount, bool shown, Texts texts)
        {
            var instances = _system.Instances;
            for (var n = 0; n < instances.Count; n++)
                _page.Live(_rows[instances[n]], _system, instances[n], recount, shown, texts);

            if (shown)
                _rows[_system].Show(texts);
        }

        /// <summary>The row for <paramref name="key" />: one of the logic systems made from the blueprint, or the blueprint itself for the way to add another.</summary>
        StatusRow Made(object key)
        {
            var instance = key as SystemInstance;
            if (instance == null)
            {
                return new StatusRow
                {
                    IsAction = true,
                    Depth = 1,
                    AboutArguments = _named,
                    Opens = _editor.Act(Cmd.AddInstance, delegate { _page.Another(_system); })
                };
            }

            var row = new StatusRow
            {
                Depth = 1,
                Opens = _editor.Act(Cmd.OpenSystem, delegate { _page.Open(_system, instance); }),
                Deletes = _editor.Act(Cmd.DeleteInstance, delegate { _editor.DeleteInstance(_system, instance); })
            };

            _page.Switched(row, instance);
            return row;
        }
    }
}
