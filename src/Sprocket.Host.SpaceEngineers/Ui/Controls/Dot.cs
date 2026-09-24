using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>A state light.</summary>
    public class Dot : Element
    {
        /// <summary>How long each side of a dot is.</summary>
        public const float Size = Sizes.Dot;

        /// <summary>Makes a dot that is not lit.</summary>
        public Dot()
        {
            Background = Palette.Dim;
        }

        /// <summary>Whether it is lit right now.</summary>
        public bool IsLit
        {
            set { Background = value ? Palette.Live : Palette.Dim; }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Size, Size);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
        }
    }
}