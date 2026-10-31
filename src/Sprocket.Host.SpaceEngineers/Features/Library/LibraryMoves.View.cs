using Lumel;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    public partial class LibraryMoves
    {
        MoveButton _out;
        MoveButton _in;
        MoveButton _copy;

        /// <summary>Makes the buttons, top to bottom: to the library, onto this construct, and a copy.</summary>
        void CreateView()
        {
            Style = Styles.MoveStrip;
            Placement = FixedSize.Width(Sizes.MoveStrip);
            Align = Align.Center;
            Children.Add(new MoveButton(Symbols.Right).As(out _out));
            Children.Add(new MoveButton(Symbols.Left).As(out _in));
            Children.Add(new MoveButton(Symbols.Duplicate).As(out _copy));
        }
    }
}
