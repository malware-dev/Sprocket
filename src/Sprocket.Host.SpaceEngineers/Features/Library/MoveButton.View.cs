using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    public partial class MoveButton
    {
        Glyph _does;

        /// <summary>Makes the mark.</summary>
        void CreateView()
        {
            Style = Styles.Move;
            Placement = FixedSize.Size(Sizes.Move, Sizes.Move);
            Children.Add(new Glyph { Style = Styles.MoveMark }.As(out _does));
        }
    }
}
