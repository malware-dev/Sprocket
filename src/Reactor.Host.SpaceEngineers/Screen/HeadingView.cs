using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Screen
{
    public sealed class HeadingView : ComponentView
    {
        readonly HeadingComponent _component;
        readonly Label _label = Widgets.Label("", Palette.Muted, Lettering.Small);

        public HeadingView(HeadingComponent component)
        {
            _component = component;
            _label.Padding = new Thickness(Space.Line, Space.Item, 0f, Space.Row);
        }

        /// <summary>Told rather than asked: the one thing it shows announces itself.</summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _label; }
        }

        protected override void Refresh()
        {
            _label.Text = _component.Text;
        }
    }
}
