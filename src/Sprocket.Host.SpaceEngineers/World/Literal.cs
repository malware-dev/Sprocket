using System;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A value written down in a program, carrying what kind of value it is.
    /// </summary>
    /// <remarks>Made when somebody writes a sequence, and never on a tick.</remarks>
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
        public static Literal Color(byte r, byte g, byte b, byte a)
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
        public bool Flag => Value.Bits != 0;

        /// <summary>It as a whole number, for <see cref="ValueKind.Integer" />.</summary>
        public long Whole => Value.Bits;

        /// <summary>It as a real number, for <see cref="ValueKind.Real" />.</summary>
        public double Real => BitConverter.Int64BitsToDouble(Value.Bits);

        /// <summary>What it is as a number, whether it was written whole or with a point, and nought for anything else.</summary>
        /// <remarks><see cref="Real" /> read over a whole number is not the number at all.</remarks>
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
        public byte Red => (byte)(Value.Bits >> 24);

        /// <summary>The green of a color.</summary>
        public byte Green => (byte)(Value.Bits >> 16);

        /// <summary>The blue of a color.</summary>
        public byte Blue => (byte)(Value.Bits >> 8);

        /// <summary>The alpha of a color.</summary>
        public byte Alpha => (byte)Value.Bits;

        /// <summary>
        /// How it reads to a person, and to anything that wanted text.
        /// </summary>
        /// <remarks>In the invariant culture: the game's own actions are handed this, and parse it back.</remarks>
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

                return $"#{Hex(Red)}{Hex(Green)}{Hex(Blue)}{(Alpha == 255 ? "" : Hex(Alpha))}";
            }
        }

        /// <summary>
        /// This as the game will take it as <paramref name="type" />, or false where it cannot be one.
        /// </summary>
        /// <remarks>A whole number also fits a real type, and a value must be within what the type holds: 300 is no byte.</remarks>
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
        /// <remarks>A whole number also becomes a real one, and a name also becomes text; every other kind only becomes itself.</remarks>
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
