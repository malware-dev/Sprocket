using Lumel.Styling;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>Ways of giving a look the words of another control.</summary>
    public static class TextLooks
    {
        /// <summary>Gives <paramref name="look" /> the face, size and color given, touching only what differs, so a look already dressed so is left unchanged.</summary>
        public static void Wear(this TextLook look, string font, float fontSize, Color color)
        {
            if (look.Font != font)
                look.Font = font;

            if (look.FontSize != fontSize)
                look.FontSize = fontSize;

            if (look.Color != color)
                look.Color = color;
        }
    }
}
