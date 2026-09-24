using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Look
{
    /// <summary>
    /// A colour as hue, saturation and value: the way one is chosen rather than the way one is held.
    /// </summary>
    /// <remarks>
    /// The game's arrangement rather than a preference. Its own colour picker's three sliders are
    /// these three, hue multiplied out to 360 and the other two from zero to one, which is the one
    /// place the two halves disagree with each other.
    /// <para>
    /// The conversions are the game's, from <c>VRageMath.ColorExtensions</c>, so a colour chosen here
    /// and a colour chosen in the painter land on the same bytes.
    /// </para>
    /// </remarks>
    public struct Hsv
    {
        Hsv(float hue, float saturation, float value) : this()
        {
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        /// <summary>Where round the wheel, from zero to one, red at both ends.</summary>
        public float Hue { get; private set; }

        /// <summary>How much of the hue there is, from white at zero.</summary>
        public float Saturation { get; private set; }

        /// <summary>How much light there is, from black at zero.</summary>
        public float Value { get; private set; }

        public static Hsv Of(float hue, float saturation, float value)
        {
            return new Hsv(Held(hue), Held(saturation), Held(value));
        }

        public static Hsv Of(Vector3 said)
        {
            return Of(said.X, said.Y, said.Z);
        }

        public static Hsv Of(Color colour)
        {
            return Of(colour.ColorToHSV());
        }

        /// <summary>
        /// One of the colours a player paints with, which is not held as one of these.
        /// </summary>
        /// <remarks>
        /// <b>A build colour slot is an offset, not a colour.</b> Its saturation and its value are
        /// shifted so that nought means the ordinary grey of unpainted armour rather than black, and
        /// both run from minus one to one. Read as if they were a colour, a whole palette comes out
        /// black, which is what happened.
        /// <para>
        /// The shift is the game's, and so is the arithmetic that undoes it: its own colour picker
        /// puts every slot through <c>HSVOffsetToHSV</c> before it draws the swatch. Using theirs
        /// rather than copying the numbers is what stops the two drifting apart.
        /// </para>
        /// </remarks>
        public static Hsv Painted(Vector3 slot)
        {
            return Of(MyColorPickerConstants.HSVOffsetToHSV(slot));
        }

        /// <summary>The same colour with one of the three moved, for a slider that owns one axis.</summary>
        public Hsv Hued(float hue)
        {
            return Of(hue, Saturation, Value);
        }

        public Hsv Saturated(float saturation)
        {
            return Of(Hue, saturation, Value);
        }

        public Hsv Valued(float value)
        {
            return Of(Hue, Saturation, value);
        }

        public Color Colour
        {
            get { return new Vector3(Hue, Saturation, Value).HSVtoColor(); }
        }

        static float Held(float part)
        {
            return part < 0f ? 0f : part > 1f ? 1f : part;
        }
    }
}
