using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Blueprints
{
    /// <summary>The line saying whose blocks a blueprint is edited through, with the name as a slot to press.</summary>
    /// <remarks>
    /// A slot, because pressing it changes what every block on the screen below it means - which is what a
    /// slot is for, and nothing else wears that look.
    /// </remarks>
    public sealed class UsingView : ComponentView
    {
        readonly UsingComponent _component;
        readonly Stack _line = Widgets.RowOf(Space.Item);
        readonly Label _lead = Widgets.Label("", Palette.Muted, Lettering.Body);
        readonly Slot _name = new Slot();

        public UsingView(UsingComponent component)
        {
            _component = component;
            _lead.Wraps = false;
            _line.Children.Add(_lead);
            _line.Children.Add(_name);
            _line.Padding = new Thickness(Space.Line, Space.Item, 0f, Space.Item);
        }

        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _line; }
        }

        protected override void Refresh()
        {
            _lead.Text = _component.Lead;
            _name.Text = _component.Name;
            _name.Press = Deed.Sends(_component.Command);

            // A notch off the page where there is no other to choose, the way a sentence's slot is that
            // cannot be pressed: the name still reads as the one thing it is.
            bool pressable = _component.Command != 0;
            _name.Background = pressable ? Palette.SlotFill : Palette.Chrome;
            _name.Underline = pressable ? Palette.SlotEdge : Palette.None;
        }
    }
}
