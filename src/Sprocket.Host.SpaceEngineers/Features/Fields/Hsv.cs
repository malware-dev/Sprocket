using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A color as hue, saturation and value.</summary>
    /// <remarks>The conversions are the game's, from <c>VRageMath.ColorExtensions</c>.</remarks>
    public struct Hsv
    {
        Hsv(float hue, float saturation, float value) : this()
        {
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        /// <summary>Where round the wheel, from zero to one, red at both ends.</summary>
        public float Hue { get; }

        /// <summary>How much of the hue there is, from white at zero.</summary>
        public float Saturation { get; }

        /// <summary>How much light there is, from black at zero.</summary>
        public float Value { get; }

        /// <summary>A color from its three parts, each held to between zero and one.</summary>
        public static Hsv Of(float hue, float saturation, float value)
        {
            return new Hsv(Held(hue), Held(saturation), Held(value));
        }

        /// <summary>A color whose hue, saturation and value are <paramref name="said" />'s X, Y and Z.</summary>
        public static Hsv Of(Vector3 said)
        {
            return Of(said.X, said.Y, said.Z);
        }

        /// <summary><paramref name="colour" /> as hue, saturation and value, by the game's conversion.</summary>
        public static Hsv Of(Color colour)
        {
            return Of(colour.ColorToHSV());
        }

        /// <summary>One of the colors a player paints with.</summary>
        /// <remarks>
        /// A build color slot is an offset whose saturation and value run from minus one to one, nought at the gray of unpainted
        /// armor, which the game's color picker undoes with <c>HSVOffsetToHSV</c>.
        /// </remarks>
        public static Hsv Painted(Vector3 slot)
        {
            return Of(MyColorPickerConstants.HSVOffsetToHSV(slot));
        }

        /// <summary>This color at the hue <paramref name="hue" />.</summary>
        public Hsv Hued(float hue)
        {
            return Of(hue, Saturation, Value);
        }

        /// <summary>This color at the saturation <paramref name="saturation" />.</summary>
        public Hsv Saturated(float saturation)
        {
            return Of(Hue, saturation, Value);
        }

        /// <summary>This color at the value <paramref name="value" />.</summary>
        public Hsv Valued(float value)
        {
            return Of(Hue, Saturation, value);
        }

        /// <summary>This color as red, green and blue, by the game's conversion.</summary>
        public Color Colour => new Vector3(Hue, Saturation, Value).HSVtoColor();

        static float Held(float part)
        {
            return part < 0f ? 0f : part > 1f ? 1f : part;
        }
    }
}