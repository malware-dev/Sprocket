using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Screen
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
