using System.Globalization;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// A value written down in a program, carrying what kind of value it is.
    /// </summary>
    /// <remarks>
    /// An <see cref="EntityValue" /> is eight bytes and says nothing about itself, because a reading
    /// always comes from a declared property and the declaration is what knows the kind. A value
    /// somebody wrote into a step has no declaration behind it: the action it is for declares how
    /// many values it wants and not one thing about what they are, and the block may not even be
    /// present when the program is read. So this one carries its kind.
    /// <para>
    /// Text is a field rather than a slot in <see cref="Values" /> because a step's values belong to
    /// the step. They are made when somebody writes a sequence and never on a tick.
    /// </para>
    /// </remarks>
    public struct Literal
    {
        Literal(ValueKind kind, EntityValue value, string text) : this()
        {
            Kind = kind;
            Value = value;
            Text = text ?? "";
        }

        public static Literal Of(bool flag)
        {
            return new Literal(ValueKind.Flag, EntityValue.Of(flag), null);
        }

        public static Literal Of(long whole)
        {
            return new Literal(ValueKind.Integer, EntityValue.Of(whole), null);
        }

        public static Literal Of(double real)
        {
            return new Literal(ValueKind.Real, EntityValue.Of(real), null);
        }

        public static Literal Of(string text)
        {
            return new Literal(ValueKind.Text, new EntityValue(0), text);
        }

        public static Literal Colour(byte r, byte g, byte b, byte a)
        {
            return new Literal(ValueKind.Color, EntityValue.Of(r, g, b, a), null);
        }

        public ValueKind Kind { get; private set; }

        /// <summary>What it is, for every kind but <see cref="ValueKind.Text" />.</summary>
        public EntityValue Value { get; private set; }

        /// <summary>The text, for <see cref="ValueKind.Text" />, and empty for the rest.</summary>
        public string Text { get; private set; }

        public bool Flag
        {
            get { return Value.Bits != 0; }
        }

        public long Whole
        {
            get { return Value.Bits; }
        }

        public double Real
        {
            get { return System.BitConverter.Int64BitsToDouble(Value.Bits); }
        }

        public byte Red
        {
            get { return (byte)(Value.Bits >> 24); }
        }

        public byte Green
        {
            get { return (byte)(Value.Bits >> 16); }
        }

        public byte Blue
        {
            get { return (byte)(Value.Bits >> 8); }
        }

        public byte Alpha
        {
            get { return (byte)Value.Bits; }
        }

        /// <summary>
        /// How it reads to a person, and to anything that wanted text.
        /// </summary>
        /// <remarks>
        /// Invariant throughout, because it is not only what a sentence shows: the game's own actions
        /// nearly all declare their values as text and parse them back, so this is what they are
        /// handed. A number that read as <c>0,25</c> on a Norwegian machine would be a value that
        /// worked on one ship and not on another.
        /// </remarks>
        public string Said
        {
            get
            {
                if (Kind == ValueKind.Text)
                    return Text;

                if (Kind == ValueKind.Flag)
                    return Flag ? "on" : "off";

                if (Kind == ValueKind.Integer)
                    return Whole.ToString(CultureInfo.InvariantCulture);

                if (Kind == ValueKind.Real)
                    return Real.ToString("R", CultureInfo.InvariantCulture);

                return "#" + Hex(Red) + Hex(Green) + Hex(Blue)
                    + (Alpha == 255 ? "" : Hex(Alpha));
            }
        }

        static string Hex(byte value)
        {
            return value.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
