using VRage.Game;
using VRage.Utils;
using VRageMath;
using Phosphor.Graphics;
using Phosphor.Ui;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Controls
{
    /// <summary>
    /// A state light.
    /// </summary>
    /// <remarks>
    /// Square rather than round, which is not a decision: the seam draws sprites by material id, there
    /// is no round one declared, and inventing a rounded primitive would put a shape into the graphics
    /// abstraction that an <c>IMyTextSurface</c> would have to reproduce. A circular texture is the
    /// answer when this is worth doing properly.
    /// </remarks>
    public class Dot : Element
    {
        public const float Size = Sizes.Dot;

        public Dot()
        {
            Background = Palette.Dim;
        }

        /// <summary>Whether it is lit right now.</summary>
        public bool IsLit
        {
            set { Background = value ? Palette.Live : Palette.Dim; }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Size, Size);
        }

        protected override void DrawContent(IGraphics g)
        {
        }
    }
}
