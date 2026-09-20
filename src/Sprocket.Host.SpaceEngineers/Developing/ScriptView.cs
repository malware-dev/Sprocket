using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Screen;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Developing
{
    /// <summary>The script, on a screed that fills the body and moves both ways.</summary>
    /// <remarks>
    /// The listing is read rather than worked in, so it is deliberately plain: the program's own text,
    /// at the size a row of the editor is set in, on the page's own ground.
    /// </remarks>
    public sealed class ScriptView : ComponentView
    {
        readonly ScriptComponent _component;
        readonly Screed _screed = new Screed();

        public ScriptView(ScriptComponent component)
        {
            _component = component;

            // The face is left as it comes. Changing it is one property, and the white one is the only
            // face the harness can measure, so a listing shot for a reference frame stays honest.
            _screed.Scale = Lettering.Small;
            _screed.Color = Palette.Ink;

            // A listing is talked about by line number - a parser says which line it could not read -
            // so the numbers are the thing that makes it answerable rather than merely readable.
            _screed.Numbered = true;
            _screed.NumberColor = Palette.Dim;
            _screed.Padding = new Thickness(Space.PadX, Space.Item, Space.Item, Space.Item);

            _screed.BarWidth = Sizes.Bar;
            _screed.BarMinLength = Sizes.Thumb;
            _screed.BarColor = Palette.Thumb;
            _screed.BarHeldColor = Palette.ThumbHeld;
        }

        public override Element Root
        {
            get { return _screed; }
        }

        /// <summary>
        /// It is the whole body and does its own scrolling, both ways.
        /// </summary>
        /// <remarks>
        /// Wrapped in the screen's scroller instead, the two would fight over one gesture, and the
        /// sideways half would have nowhere to live at all.
        /// </remarks>
        public override bool ScrollsItself
        {
            get { return true; }
        }

        public override bool TryGrab(Vector2 point)
        {
            return _screed.Take(point);
        }

        public override void Carry(Vector2 point)
        {
            _screed.Carry(point);
        }

        public override void Drop()
        {
            _screed.Drop();
        }

        public override bool ScrollBy(Vector2 point, float amount)
        {
            if (!_screed.Reaches(point) || !_screed.CanMove(Way.Down))
                return false;

            _screed.MoveBy(Way.Down, amount);
            return true;
        }

        protected override void Refresh()
        {
            _screed.Text = _component.Text;
        }
    }
}
