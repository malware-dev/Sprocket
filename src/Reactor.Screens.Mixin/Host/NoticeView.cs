using System.Collections.Generic;
using Reactor.Ui;
using VRageMath;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Host
{
    public sealed class NoticeView : ComponentView
    {
        readonly NoticeComponent _component;
        readonly Label _label = Widgets.Wash("", Palette.Problem, Palette.ProblemWash);

        public NoticeView(NoticeComponent component)
        {
            _component = component;
        }

        public override Element Root
        {
            get { return _label; }
        }

        public override void Update(int tick)
        {
            bool problem = _component.Level == NoticeLevel.Problem;

            _label.Text = _component.Text;
            _label.Color = problem ? Palette.Problem : Palette.Notice;
            _label.Background = problem ? Palette.ProblemWash : Palette.NoticeWash;
        }
    }
}
