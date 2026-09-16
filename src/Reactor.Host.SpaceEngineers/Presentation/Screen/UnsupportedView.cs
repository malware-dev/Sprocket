using Reactor.Host.SpaceEngineers.Core.Ui;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Look;

namespace Reactor.Host.SpaceEngineers.Presentation.Screen
{
    /// <summary>A component with no renderer yet, drawn so that it cannot be mistaken for absence.</summary>
    public sealed class UnsupportedView : ComponentView
    {
        readonly Label _label;

        public UnsupportedView(EditorComponent component)
        {
            _label = Widgets.Wash(
                component.GetType().Name + " is not drawn on this target yet",
                Palette.Ink, new Color(220, 60, 190));
        }

        public override Element Root
        {
            get { return _label; }
        }

        public override void Update(int tick)
        {
        }
    }
}
