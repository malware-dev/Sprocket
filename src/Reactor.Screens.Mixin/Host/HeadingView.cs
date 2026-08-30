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
    public sealed class HeadingView : ComponentView
    {
        readonly HeadingComponent _component;
        readonly Label _label = Widgets.Label("", Palette.Muted, Lettering.Small);

        public HeadingView(HeadingComponent component)
        {
            _component = component;
            _label.Padding = new Thickness(Space.Line, Space.Item, 0f, Space.Row);
        }

        public override Element Root
        {
            get { return _label; }
        }

        public override void Update(int tick)
        {
            _label.Text = _component.Text;
        }
    }
}
