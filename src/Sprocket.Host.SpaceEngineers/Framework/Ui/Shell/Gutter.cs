using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>What something holds, set in under it, with a spine down the middle of the room before it that turns in at the last thing shown.</summary>
    public class Gutter : HStack
    {
        readonly TreeLine _spine;
        readonly VStack _body;

        /// <summary>Creates a gutter <paramref name="width" /> wide whose body puts <paramref name="spacing" /> between the things it holds.</summary>
        public Gutter(float width, float spacing)
        {
            var side = (width - Sizes.Spine) / 2f;
            Base.Spacing = side;
            Base.Padding = new Thickness(side, 0f, 0f, 0f);

            _spine = new TreeLine { Placement = FixedSize.Width(Sizes.Spine) };
            _body = new VStack { Base = { Spacing = spacing }, Placement = Flex.Grow };
            Children.Add(_spine);
            Children.Add(_body);
        }

        /// <summary>The things set in, top to bottom.</summary>
        public VStack Body => _body;

        /// <summary>Whether the spine turns in at the bottom of the last thing shown, for one tall thing, rather than at its middle.</summary>
        public bool Spans { get; set; }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);
            _spine.Ends = Last();
            _spine.TurnsAtBottom = Spans;
        }

        Element Last()
        {
            for (var i = _body.Children.Count - 1; i >= 0; i--)
            {
                if (_body.Children[i].IsVisible)
                    return _body.Children[i];
            }

            return null;
        }
    }
}
