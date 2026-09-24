using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Controls;

namespace Sprocket.Host.SpaceEngineers.Screen
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

        protected override void Refresh()
        {
        }
    }
}
