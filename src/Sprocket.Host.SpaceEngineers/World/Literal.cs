using System;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A value written down in a program, carrying what kind of value it is.
    /// </summary>
    /// <remarks>
    /// An <see cref="EntityValue" /> is eight bytes and says nothing about itself, because a reading
    /// always comes from a declared property and the declaration is what knows the kind. A value
    /// somebody wrote into a step is written before anything is bound to it, and the block may not
    /// even be present when the program is read. So this one carries its kind.
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

        /// <summary>A flag, on or off.</summary>
        public static Literal Of(bool flag)
        {
            return new Literal(ValueKind.Flag, EntityValue.Of(flag), null);
        }

        /// <summary>A whole number.</summary>
        public static Literal Of(long whole)
        {
            return new Literal(ValueKind.Integer, EntityValue.Of(whole), null);
        }

        /// <summary>A real number.</summary>
        public static Literal Of(double real)
        {
            return new Literal(ValueKind.Real, EntityValue.Of(real), null);
        }

        /// <summary>Text.</summary>
        public static Literal Of(string text)
        {
            return new Literal(ValueKind.Text, new EntityValue(0), text);
        }

        /// <summary>One of a list of names a member gives, written as the name.</summary>
        public static Literal Name(string name)
        {
            return new Literal(ValueKind.Name, new EntityValue(0), name);
        }

        /// <summary>A color, by its red, green, blue and alpha.</summary>
        public static Literal Colour(byte r, byte g, byte b, byte a)
        {
            return new Literal(ValueKind.Color, EntityValue.Of(r, g, b, a), null);
        }

        /// <summary>The kind of value it is.</summary>
        public ValueKind Kind { get; private set; }

        /// <summary>What it is, for every kind but <see cref="ValueKind.Text" />.</summary>
        public EntityValue Value { get; private set; }

        /// <summary>The text, for <see cref="ValueKind.Text" />, or the name, for <see cref="ValueKind.Name" />, and empty for the rest.</summary>
        public string Text { get; private set; }

        /// <summary>It as a flag, for <see cref="ValueKind.Flag" />.</summary>
        public bool Flag
        {
            get { return Value.Bits != 0; }
        }

        /// <summary>It as a whole number, for <see cref="ValueKind.Integer" />.</summary>
        public long Whole
        {
            get { return Value.Bits; }
        }

        /// <summary>It as a real number, for <see cref="ValueKind.Real" />.</summary>
        public double Real
        {
            get { return BitConverter.Int64BitsToDouble(Value.Bits); }
        }

        /// <summary>What it is as a number, whether it was written whole or with a point, and nought for anything else.</summary>
        /// <remarks>
        /// A program writes "90" as a whole number and "90.5" as a real one, and somebody typing an
        /// angle does not choose between them. <see cref="Real" /> reads the bits of a real number, and
        /// read over a whole one it is not the number at all.
        /// </remarks>
        public double Number
        {
            get
            {
                if (Kind == ValueKind.Integer)
                    return Whole;

                return Kind == ValueKind.Real ? Real : 0;
            }
        }

        /// <summary>The red of a color.</summary>
        public byte Red
        {
            get { return (byte)(Value.Bits >> 24); }
        }

        /// <summary>The green of a color.</summary>
        public byte Green
        {
            get { return (byte)(Value.Bits >> 16); }
        }

        /// <summary>The blue of a color.</summary>
        public byte Blue
        {
            get { return (byte)(Value.Bits >> 8); }
        }

        /// <summary>The alpha of a color.</summary>
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
                if (Kind == ValueKind.Text || Kind == ValueKind.Name)
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

        /// <summary>
        /// This as the game will take it as <paramref name="type" />, or false where it cannot be one.
        /// </summary>
        /// <remarks>
        /// Within the language's rule - a value is the kind it was written as, and a whole number is
        /// also a real one - and within what the type holds: 300 is no byte. A type nothing says is
        /// anything.
        /// </remarks>
        public bool Fits(Primitive type, out Literal became)
        {
            became = this;
            switch (type)
            {
                case Primitive.None:
                    return true;
                case Primitive.Boolean:
                    return Kind == ValueKind.Flag;
                case Primitive.Byte:
                    return Within(byte.MinValue, byte.MaxValue);
                case Primitive.SByte:
                    return Within(sbyte.MinValue, sbyte.MaxValue);
                case Primitive.Int16:
                    return Within(short.MinValue, short.MaxValue);
                case Primitive.UInt16:
                    return Within(ushort.MinValue, ushort.MaxValue);
                case Primitive.Int32:
                    return Within(int.MinValue, int.MaxValue);
                case Primitive.UInt32:
                    return Within(uint.MinValue, uint.MaxValue);
                case Primitive.Int64:
                    return Kind == ValueKind.Integer;
                case Primitive.UInt64:
                    return Within(0, long.MaxValue);
                case Primitive.Single:
                    return Becomes(ValueKind.Real, out became) && Math.Abs(became.Real) <= float.MaxValue;
                case Primitive.Double:
                    return Becomes(ValueKind.Real, out became);
                case Primitive.Char:
                    return (Kind == ValueKind.Text || Kind == ValueKind.Name) && Text.Length == 1;
                case Primitive.String:
                    return Kind == ValueKind.Text || Kind == ValueKind.Name;
            }

            return false;
        }

        bool Within(long least, long most)
        {
            return Kind == ValueKind.Integer && Whole >= least && Whole <= most;
        }

        /// <summary>
        /// This as a value of <paramref name="type" /> itself, boxed, for one that <see cref="Fits" />.
        /// </summary>
        /// <remarks>Made when a program is bound, never when it runs, so the boxing is paid once.</remarks>
        public object Native(Primitive type)
        {
            switch (type)
            {
                case Primitive.Boolean:
                    return Flag;
                case Primitive.Byte:
                    return (byte)Whole;
                case Primitive.SByte:
                    return (sbyte)Whole;
                case Primitive.Int16:
                    return (short)Whole;
                case Primitive.UInt16:
                    return (ushort)Whole;
                case Primitive.Int32:
                    return (int)Whole;
                case Primitive.UInt32:
                    return (uint)Whole;
                case Primitive.Int64:
                    return Whole;
                case Primitive.UInt64:
                    return (ulong)Whole;
                case Primitive.Single:
                    return (float)Number;
                case Primitive.Double:
                    return Number;
                case Primitive.Char:
                    return Text[0];
                case Primitive.String:
                    return Text;
            }

            return Said;
        }

        /// <summary>This as <paramref name="kind" />, or false where it cannot be one.</summary>
        /// <remarks>
        /// The same kind is itself, a whole number is also a real one, and a name is also text, as a bare word where text is
        /// taken. Nothing else becomes anything: a value is written as the kind it is.
        /// </remarks>
        public bool Becomes(ValueKind kind, out Literal became)
        {
            became = this;
            if (Kind == kind)
                return true;

            if (Kind == ValueKind.Name && kind == ValueKind.Text)
            {
                became = Of(Text);
                return true;
            }

            if (Kind != ValueKind.Integer || kind != ValueKind.Real)
                return false;

            became = Of((double)Whole);
            return true;
        }

        static string Hex(byte value)
        {
            return value.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
